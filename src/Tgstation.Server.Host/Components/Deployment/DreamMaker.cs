﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Byond;
using Tgstation.Server.Host.Components.Chat;
using Tgstation.Server.Host.Components.Deployment.Remote;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Components.Repository;
using Tgstation.Server.Host.Components.Session;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.Jobs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <inheritdoc />
	sealed class DreamMaker : IDreamMaker
	{
		/// <summary>
		/// Extension for .dmbs.
		/// </summary>
		public const string DmbExtension = ".dmb";

		/// <summary>
		/// Extension for .dmes.
		/// </summary>
		const string DmeExtension = "dme";

		/// <summary>
		/// The <see cref="IByondManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IByondManager byond;

		/// <summary>
		/// The <see cref="IIOManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="StaticFiles.IConfiguration"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly StaticFiles.IConfiguration configuration;

		/// <summary>
		/// The <see cref="ISessionControllerFactory"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ISessionControllerFactory sessionControllerFactory;

		/// <summary>
		/// The <see cref="IEventConsumer"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IEventConsumer eventConsumer;

		/// <summary>
		/// The <see cref="IChatManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IChatManager chatManager;

		/// <summary>
		/// The <see cref="IProcessExecutor"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IProcessExecutor processExecutor;

		/// <summary>
		/// The <see cref="IRepositoryManager"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IRepositoryManager repositoryManager;

		/// <summary>
		/// The <see cref="ICompileJobSink"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ICompileJobSink compileJobConsumer;

		/// <summary>
		/// The <see cref="IRemoteDeploymentManagerFactory"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory;

		/// <summary>
		/// The <see cref="ILogger"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly ILogger<DreamMaker> logger;

		/// <summary>
		/// The <see cref="SessionConfiguration"/> for <see cref="DreamMaker"/>.
		/// </summary>
		readonly SessionConfiguration sessionConfiguration;

		/// <summary>
		/// The <see cref="Instance"/> <see cref="DreamMaker"/> belongs to.
		/// </summary>
		readonly Api.Models.Instance metadata;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="deploying"/>.
		/// </summary>
		readonly object deploymentLock;

		/// <summary>
		/// The active callback from <see cref="IChatManager.QueueDeploymentMessage"/>.
		/// </summary>
		Action<string, string> currentChatCallback;

		/// <summary>
		/// Cached for <see cref="currentChatCallback"/>.
		/// </summary>
		string currentDreamMakerOutput;

		/// <summary>
		/// Current stage to report on the job.
		/// </summary>
		string currentStage;

		/// <summary>
		/// If a compile job is running.
		/// </summary>
		bool deploying;

		/// <summary>
		/// Format a given <see cref="Exception"/> for display to users.
		/// </summary>
		/// <param name="exception">The <see cref="Exception"/> to format.</param>
		/// <returns>An error <see cref="string"/> for end users.</returns>
		static string FormatExceptionForUsers(Exception exception)
			=> exception is OperationCanceledException
				? "The job was cancelled!"
				: exception.Message;

		/// <summary>
		/// Initializes a new instance of the <see cref="DreamMaker"/> class.
		/// </summary>
		/// <param name="byond">The value of <see cref="byond"/>.</param>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="configuration">The value of <see cref="configuration"/>.</param>
		/// <param name="sessionControllerFactory">The value of <see cref="sessionControllerFactory"/>.</param>
		/// <param name="eventConsumer">The value of <see cref="eventConsumer"/>.</param>
		/// <param name="chatManager">The value of <see cref="chatManager"/>.</param>
		/// <param name="processExecutor">The value of <see cref="processExecutor"/>.</param>
		/// <param name="compileJobConsumer">The value of <see cref="compileJobConsumer"/>.</param>
		/// <param name="repositoryManager">The value of <see cref="repositoryManager"/>.</param>
		/// <param name="remoteDeploymentManagerFactory">The value of <see cref="remoteDeploymentManagerFactory"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="sessionConfiguration">The value of <see cref="sessionConfiguration"/>.</param>
		/// <param name="metadata">The value of <see cref="metadata"/>.</param>
		public DreamMaker(
			IByondManager byond,
			IIOManager ioManager,
			StaticFiles.IConfiguration configuration,
			ISessionControllerFactory sessionControllerFactory,
			IEventConsumer eventConsumer,
			IChatManager chatManager,
			IProcessExecutor processExecutor,
			ICompileJobSink compileJobConsumer,
			IRepositoryManager repositoryManager,
			IRemoteDeploymentManagerFactory remoteDeploymentManagerFactory,
			ILogger<DreamMaker> logger,
			SessionConfiguration sessionConfiguration,
			Api.Models.Instance metadata)
		{
			this.byond = byond ?? throw new ArgumentNullException(nameof(byond));
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.sessionControllerFactory = sessionControllerFactory ?? throw new ArgumentNullException(nameof(sessionControllerFactory));
			this.eventConsumer = eventConsumer ?? throw new ArgumentNullException(nameof(eventConsumer));
			this.chatManager = chatManager ?? throw new ArgumentNullException(nameof(chatManager));
			this.processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
			this.compileJobConsumer = compileJobConsumer ?? throw new ArgumentNullException(nameof(compileJobConsumer));
			this.repositoryManager = repositoryManager ?? throw new ArgumentNullException(nameof(repositoryManager));
			this.remoteDeploymentManagerFactory = remoteDeploymentManagerFactory ?? throw new ArgumentNullException(nameof(remoteDeploymentManagerFactory));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.sessionConfiguration = sessionConfiguration ?? throw new ArgumentNullException(nameof(sessionConfiguration));
			this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));

			deploymentLock = new object();
		}

		/// <inheritdoc />
#pragma warning disable CA1506
		public async Task DeploymentProcess(
			Models.Job job,
			IDatabaseContextFactory databaseContextFactory,
			JobProgressReporter progressReporter,
			CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));
			if (databaseContextFactory == null)
				throw new ArgumentNullException(nameof(databaseContextFactory));
			if (progressReporter == null)
				throw new ArgumentNullException(nameof(progressReporter));

			lock (deploymentLock)
			{
				if (deploying)
					throw new JobException(ErrorCode.DreamMakerCompileJobInProgress);
				deploying = true;
			}

			currentChatCallback = null;
			currentDreamMakerOutput = null;
			Models.CompileJob compileJob = null;
			try
			{
				string repoOwner = null;
				string repoName = null;
				TimeSpan? averageSpan = null;
				Models.RepositorySettings repositorySettings = null;
				Models.DreamDaemonSettings ddSettings = null;
				Models.DreamMakerSettings dreamMakerSettings = null;
				IRepository repo = null;
				IRemoteDeploymentManager remoteDeploymentManager = null;
				Models.RevisionInformation revInfo = null;
				await databaseContextFactory.UseContext(
					async databaseContext =>
					{
						averageSpan = await CalculateExpectedDeploymentTime(databaseContext, cancellationToken);

						ddSettings = await databaseContext
							.DreamDaemonSettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.Select(x => new Models.DreamDaemonSettings
							{
								StartupTimeout = x.StartupTimeout,
							})
							.FirstOrDefaultAsync(cancellationToken)
							;
						if (ddSettings == default)
							throw new JobException(ErrorCode.InstanceMissingDreamDaemonSettings);

						dreamMakerSettings = await databaseContext
							.DreamMakerSettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.FirstAsync(cancellationToken)
							;
						if (dreamMakerSettings == default)
							throw new JobException(ErrorCode.InstanceMissingDreamMakerSettings);

						repositorySettings = await databaseContext
							.RepositorySettings
							.AsQueryable()
							.Where(x => x.InstanceId == metadata.Id)
							.Select(x => new Models.RepositorySettings
							{
								AccessToken = x.AccessToken,
								AccessUser = x.AccessUser,
								ShowTestMergeCommitters = x.ShowTestMergeCommitters,
								PushTestMergeCommits = x.PushTestMergeCommits,
								PostTestMergeComment = x.PostTestMergeComment,
							})
							.FirstOrDefaultAsync(cancellationToken)
							;
						if (repositorySettings == default)
							throw new JobException(ErrorCode.InstanceMissingRepositorySettings);

						repo = await repositoryManager.LoadRepository(cancellationToken);
						try
						{
							if (repo == null)
								throw new JobException(ErrorCode.RepoMissing);

							remoteDeploymentManager = remoteDeploymentManagerFactory
								.CreateRemoteDeploymentManager(metadata, repo.RemoteGitProvider.Value);

							var repoSha = repo.Head;
							repoOwner = repo.RemoteRepositoryOwner;
							repoName = repo.RemoteRepositoryName;
							revInfo = await databaseContext
								.RevisionInformations
								.AsQueryable()
								.Where(x => x.CommitSha == repoSha && x.Instance.Id == metadata.Id)
								.Include(x => x.ActiveTestMerges)
									.ThenInclude(x => x.TestMerge)
									.ThenInclude(x => x.MergedBy)
								.FirstOrDefaultAsync(cancellationToken)
								;

							if (revInfo == default)
							{
								revInfo = new Models.RevisionInformation
								{
									CommitSha = repoSha,
									Timestamp = await repo.TimestampCommit(repoSha, cancellationToken),
									OriginCommitSha = repoSha,
									Instance = new Models.Instance
									{
										Id = metadata.Id,
									},
									ActiveTestMerges = new List<RevInfoTestMerge>(),
								};

								logger.LogInformation(Repository.Repository.OriginTrackingErrorTemplate, repoSha);
								databaseContext.RevisionInformations.Add(revInfo);
								databaseContext.Instances.Attach(revInfo.Instance);
								await databaseContext.Save(cancellationToken);
							}
						}
						catch
						{
							repo?.Dispose();
							throw;
						}
					})
					;

				var likelyPushedTestMergeCommit =
					repositorySettings.PushTestMergeCommits.Value
					&& repositorySettings.AccessToken != null
					&& repositorySettings.AccessUser != null;
				using (repo)
					compileJob = await Compile(
						revInfo,
						dreamMakerSettings,
						ddSettings.StartupTimeout.Value,
						repo,
						remoteDeploymentManager,
						progressReporter,
						averageSpan,
						likelyPushedTestMergeCommit,
						cancellationToken)
						;

				var activeCompileJob = compileJobConsumer.LatestCompileJob();
				try
				{
					await databaseContextFactory.UseContext(
						async databaseContext =>
						{
							var fullJob = compileJob.Job;
							compileJob.Job = new Models.Job
							{
								Id = job.Id,
							};
							var fullRevInfo = compileJob.RevisionInformation;
							compileJob.RevisionInformation = new Models.RevisionInformation
							{
								Id = revInfo.Id,
							};

							databaseContext.Jobs.Attach(compileJob.Job);
							databaseContext.RevisionInformations.Attach(compileJob.RevisionInformation);
							databaseContext.CompileJobs.Add(compileJob);

							// The difficulty with compile jobs is they have a two part commit
							await databaseContext.Save(cancellationToken);
							logger.LogTrace("Created CompileJob {0}", compileJob.Id);
							try
							{
								await compileJobConsumer.LoadCompileJob(compileJob, cancellationToken);
							}
							catch
							{
								// So we need to un-commit the compile job if the above throws
								databaseContext.CompileJobs.Remove(compileJob);

								// DCT: Cancellation token is for job, operation must run regardless
								await databaseContext.Save(default);
								throw;
							}

							compileJob.Job = fullJob;
							compileJob.RevisionInformation = fullRevInfo;
						})
						;
				}
				catch (Exception ex)
				{
					await CleanupFailedCompile(compileJob, remoteDeploymentManager, ex);
					throw;
				}

				var commentsTask = remoteDeploymentManager.PostDeploymentComments(
					compileJob,
					activeCompileJob?.RevisionInformation,
					repositorySettings,
					repoOwner,
					repoName,
					cancellationToken);

				var eventTask = eventConsumer.HandleEvent(EventType.DeploymentComplete, Enumerable.Empty<string>(), cancellationToken);

				try
				{
					currentChatCallback(null, compileJob.Output);

					await Task.WhenAll(commentsTask, eventTask);
				}
				catch (Exception ex)
				{
					throw new JobException(ErrorCode.PostDeployFailure, ex);
				}
				finally
				{
					currentChatCallback = null;
				}
			}
			catch (Exception ex)
			{
				currentChatCallback?.Invoke(
					FormatExceptionForUsers(ex),
					currentDreamMakerOutput);

				throw;
			}
			finally
			{
				deploying = false;
			}
		}
#pragma warning restore CA1506

		/// <summary>
		/// Calculate the average length of a deployment using a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to retrieve previous deployment <see cref="Job"/>s from.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the average <see cref="TimeSpan"/> of the 10 previous deployments or <see langword="null"/> if there are none.</returns>
		async Task<TimeSpan?> CalculateExpectedDeploymentTime(IDatabaseContext databaseContext, CancellationToken cancellationToken)
		{
			var previousCompileJobs = await databaseContext
				.CompileJobs
				.AsQueryable()
				.Where(x => x.Job.Instance.Id == metadata.Id)
				.OrderByDescending(x => x.Job.StoppedAt)
				.Take(10)
				.Select(x => new Models.Job
				{
					StoppedAt = x.Job.StoppedAt,
					StartedAt = x.Job.StartedAt,
				})
				.ToListAsync(cancellationToken)
				;

			TimeSpan? averageSpan = null;
			if (previousCompileJobs.Count != 0)
			{
				var totalSpan = TimeSpan.Zero;
				foreach (var previousCompileJob in previousCompileJobs)
					totalSpan += previousCompileJob.StoppedAt.Value - previousCompileJob.StartedAt.Value;
				averageSpan = totalSpan / previousCompileJobs.Count;
			}

			return averageSpan;
		}

		/// <summary>
		/// Run the compile implementation.
		/// </summary>
		/// <param name="revisionInformation">The <see cref="RevisionInformation"/>.</param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.Internal.DreamMakerSettings"/>.</param>
		/// <param name="apiValidateTimeout">The API validation timeout.</param>
		/// <param name="repository">The <see cref="IRepository"/>.</param>
		/// <param name="remoteDeploymentManager">The <see cref="IRemoteDeploymentManager"/>.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="estimatedDuration">The optional estimated <see cref="TimeSpan"/> of the compilation.</param>
		/// <param name="localCommitExistsOnRemote">Whether or not the <paramref name="repository"/>'s current commit exists on the remote repository.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the completed <see cref="CompileJob"/>.</returns>
		async Task<Models.CompileJob> Compile(
			Models.RevisionInformation revisionInformation,
			Api.Models.Internal.DreamMakerSettings dreamMakerSettings,
			uint apiValidateTimeout,
			IRepository repository,
			IRemoteDeploymentManager remoteDeploymentManager,
			JobProgressReporter progressReporter,
			TimeSpan? estimatedDuration,
			bool localCommitExistsOnRemote,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Begin Compile");

			using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			currentStage = "Reserving BYOND version";
			var progressTask = ProgressTask(progressReporter, estimatedDuration, progressCts.Token);
			try
			{
				using var byondLock = await byond.UseExecutables(null, cancellationToken);
				currentChatCallback = chatManager.QueueDeploymentMessage(
					revisionInformation,
					byondLock.Version,
					DateTimeOffset.UtcNow + estimatedDuration,
					repository.RemoteRepositoryOwner,
					repository.RemoteRepositoryName,
					localCommitExistsOnRemote);

				var job = new Models.CompileJob
				{
					DirectoryName = Guid.NewGuid(),
					DmeName = dreamMakerSettings.ProjectName,
					RevisionInformation = revisionInformation,
					ByondVersion = byondLock.Version.ToString(),
					RepositoryOrigin = repository.Origin.ToString(),
				};

				currentStage = "Creating remote deployment notification";
				await remoteDeploymentManager.StartDeployment(
					repository,
					job,
					cancellationToken)
					;

				logger.LogTrace("Deployment will timeout at {timeoutTime}", DateTimeOffset.UtcNow + dreamMakerSettings.Timeout.Value);
				using var timeoutTokenSource = new CancellationTokenSource(dreamMakerSettings.Timeout.Value);
				var timeoutToken = timeoutTokenSource.Token;
				using (timeoutToken.Register(() => logger.LogWarning("Deployment timed out!")))
				{
					using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutToken, cancellationToken);
					try
					{
						await RunCompileJob(
							job,
							dreamMakerSettings,
							byondLock,
							repository,
							remoteDeploymentManager,
							apiValidateTimeout,
							combinedTokenSource.Token)
							;
					}
					catch (OperationCanceledException) when (timeoutToken.IsCancellationRequested)
					{
						throw new JobException(ErrorCode.DeploymentTimeout);
					}
				}

				return job;
			}
			catch (OperationCanceledException)
			{
				// DCT: Cancellation token is for job, delaying here is fine
				currentStage = "Running CompileCancelled event";
				await eventConsumer.HandleEvent(EventType.CompileCancelled, Enumerable.Empty<string>(), default);
				throw;
			}
			finally
			{
				progressCts.Cancel();
				await progressTask;
			}
		}

		/// <summary>
		/// Executes and populate a given <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> to run and populate.</param>
		/// <param name="dreamMakerSettings">The <see cref="Api.Models.Internal.DreamMakerSettings"/> to use.</param>
		/// <param name="byondLock">The <see cref="IByondExecutableLock"/> to use.</param>
		/// <param name="repository">The <see cref="IRepository"/> to use.</param>
		/// <param name="remoteDeploymentManager">The <see cref="IRemoteDeploymentManager"/> to use.</param>
		/// <param name="apiValidateTimeout">The timeout for validating the DMAPI.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunCompileJob(
			Models.CompileJob job,
			Api.Models.Internal.DreamMakerSettings dreamMakerSettings,
			IByondExecutableLock byondLock,
			IRepository repository,
			IRemoteDeploymentManager remoteDeploymentManager,
			uint apiValidateTimeout,
			CancellationToken cancellationToken)
		{
			var outputDirectory = job.DirectoryName.ToString();
			logger.LogTrace("Compile output GUID: {0}", outputDirectory);

			try
			{
				// copy the repository
				logger.LogTrace("Copying repository to game directory");
				currentStage = "Copying repository";
				var resolvedOutputDirectory = ioManager.ResolvePath(outputDirectory);
				var repoOrigin = repository.Origin;
				using (repository)
					await repository.CopyTo(resolvedOutputDirectory, cancellationToken);

				// repository closed now

				// run precompile scripts
				currentStage = "Running PreCompile event";
				await eventConsumer.HandleEvent(
					EventType.CompileStart,
					new List<string>
					{
						resolvedOutputDirectory,
						repoOrigin.ToString(),
						$"{byondLock.Version.Major}.{byondLock.Version.Minor}",
					},
					cancellationToken)
					;

				// determine the dme
				currentStage = "Determining .dme";
				if (job.DmeName == null)
				{
					logger.LogTrace("Searching for available .dmes");
					var foundPaths = await ioManager.GetFilesWithExtension(resolvedOutputDirectory, DmeExtension, true, cancellationToken);
					var foundPath = foundPaths.FirstOrDefault();
					if (foundPath == default)
						throw new JobException(ErrorCode.DreamMakerNoDme);
					job.DmeName = foundPath.Substring(
						resolvedOutputDirectory.Length + 1,
						foundPath.Length - resolvedOutputDirectory.Length - DmeExtension.Length - 2); // +1 for . in extension
				}
				else
				{
					var targetDme = ioManager.ConcatPath(outputDirectory, String.Join('.', job.DmeName, DmeExtension));
					var targetDmeExists = await ioManager.FileExists(targetDme, cancellationToken);
					if (!targetDmeExists)
						throw new JobException(ErrorCode.DreamMakerMissingDme);
				}

				logger.LogDebug("Selected {0}.dme for compilation!", job.DmeName);

				currentStage = "Modifying .dme";
				await ModifyDme(job, cancellationToken);

				// run precompile scripts
				currentStage = "Running PreDreamMaker event";
				await eventConsumer.HandleEvent(
					EventType.PreDreamMaker,
					new List<string>
					{
						resolvedOutputDirectory,
						repoOrigin.ToString(),
						$"{byondLock.Version.Major}.{byondLock.Version.Minor}",
					},
					cancellationToken)
					;

				// run compiler
				currentStage = "Running DreamMaker";
				var exitCode = await RunDreamMaker(byondLock.DreamMakerPath, job, cancellationToken);

				// verify api
				try
				{
					if (exitCode != 0)
						throw new JobException(
							ErrorCode.DreamMakerExitCode,
							new JobException($"Exit code: {exitCode}{Environment.NewLine}{Environment.NewLine}{job.Output}"));

					currentStage = "Validating DMAPI";
					await VerifyApi(
						apiValidateTimeout,
						dreamMakerSettings.ApiValidationSecurityLevel.Value,
						job,
						byondLock,
						dreamMakerSettings.ApiValidationPort.Value,
						dreamMakerSettings.RequireDMApiValidation.Value,
						cancellationToken)
						;
				}
				catch (JobException)
				{
					// DD never validated or compile failed
					currentStage = "Running CompileFailure event";
					await eventConsumer.HandleEvent(
						EventType.CompileFailure,
						new List<string>
						{
							resolvedOutputDirectory,
							exitCode == 0 ? "1" : "0",
							$"{byondLock.Version.Major}.{byondLock.Version.Minor}",
						},
						cancellationToken)
						;
					throw;
				}

				currentStage = "Running CompileComplete event";
				await eventConsumer.HandleEvent(
					EventType.CompileComplete,
					new List<string>
					{
						resolvedOutputDirectory,
						$"{byondLock.Version.Major}.{byondLock.Version.Minor}",
					},
					cancellationToken)
					;

				logger.LogTrace("Applying static game file symlinks...");
				currentStage = "Symlinking GameStaticFiles";

				// symlink in the static data
				await configuration.SymlinkStaticFilesTo(resolvedOutputDirectory, cancellationToken);

				logger.LogDebug("Compile complete!");
			}
			catch (Exception ex)
			{
				currentStage = "Cleaning output directory";
				await CleanupFailedCompile(job, remoteDeploymentManager, ex);
				throw;
			}
		}

		/// <summary>
		/// Gradually triggers a given <paramref name="progressReporter"/> over a given <paramref name="estimatedDuration"/>.
		/// </summary>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> to report progress of the operation.</param>
		/// <param name="estimatedDuration">A <see cref="TimeSpan"/> representing the duration to give progress over if any.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task ProgressTask(JobProgressReporter progressReporter, TimeSpan? estimatedDuration, CancellationToken cancellationToken)
		{
			var noEstimate = !estimatedDuration.HasValue;
			progressReporter.StageName = currentStage;
			progressReporter.ReportProgress(noEstimate ? null : 0);

			var sleepInterval = estimatedDuration.HasValue ? estimatedDuration.Value / 100 : TimeSpan.FromMilliseconds(250);

			logger.LogDebug("Compile is expected to take: {0}", estimatedDuration);
			try
			{
				for (var iteration = 0; iteration < (estimatedDuration.HasValue ? 99 : Int32.MaxValue); ++iteration)
				{
					await Task.Delay(sleepInterval, cancellationToken);
					progressReporter.StageName = currentStage;
					progressReporter.ReportProgress(noEstimate ? null : sleepInterval * (iteration + 1) / estimatedDuration.Value);
				}
			}
			catch (OperationCanceledException)
			{
			}
		}

		/// <summary>
		/// Run a quick DD instance to test the DMAPI is installed on the target code.
		/// </summary>
		/// <param name="timeout">The timeout in seconds for validation.</param>
		/// <param name="securityLevel">The <see cref="DreamDaemonSecurity"/> level to use to validate the API.</param>
		/// <param name="job">The <see cref="CompileJob"/> for the operation.</param>
		/// <param name="byondLock">The current <see cref="IByondExecutableLock"/>.</param>
		/// <param name="portToUse">The port to use for API validation.</param>
		/// <param name="requireValidate">If the API validation is required to complete the deployment.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task VerifyApi(
			uint timeout,
			DreamDaemonSecurity securityLevel,
			Models.CompileJob job,
			IByondExecutableLock byondLock,
			ushort portToUse,
			bool requireValidate,
			CancellationToken cancellationToken)
		{
			logger.LogTrace("Verifying {0}DMAPI...", requireValidate ? "required " : String.Empty);
			var launchParameters = new DreamDaemonLaunchParameters
			{
				AllowWebClient = false,
				Port = portToUse,
				SecurityLevel = securityLevel,
				Visibility = DreamDaemonVisibility.Invisible,
				StartupTimeout = timeout,
				TopicRequestTimeout = 0, // not used
				HeartbeatSeconds = 0, // not used
				StartProfiler = false,
			};

			job.MinimumSecurityLevel = securityLevel; // needed for the TempDmbProvider

			ApiValidationStatus validationStatus;
			using (var provider = new TemporaryDmbProvider(ioManager.ResolvePath(job.DirectoryName.ToString()), String.Concat(job.DmeName, DmbExtension), job))
			await using (var controller = await sessionControllerFactory.LaunchNew(provider, byondLock, launchParameters, true, cancellationToken))
			{
				controller.AdjustPriority(false);

				var launchResult = await controller.LaunchResult;

				if (launchResult.StartupTime.HasValue)
					await controller.Lifetime.WithToken(cancellationToken);

				if (!controller.Lifetime.IsCompleted)
					await controller.DisposeAsync();

				validationStatus = controller.ApiValidationStatus;

				if (requireValidate && validationStatus == ApiValidationStatus.NeverValidated)
					throw new JobException(ErrorCode.DreamMakerNeverValidated);

				logger.LogTrace("API validation status: {0}", validationStatus);

				job.DMApiVersion = controller.DMApiVersion;
			}

			switch (validationStatus)
			{
				case ApiValidationStatus.RequiresUltrasafe:
					job.MinimumSecurityLevel = DreamDaemonSecurity.Ultrasafe;
					return;
				case ApiValidationStatus.RequiresSafe:
					job.MinimumSecurityLevel = DreamDaemonSecurity.Safe;
					return;
				case ApiValidationStatus.RequiresTrusted:
					job.MinimumSecurityLevel = DreamDaemonSecurity.Trusted;
					return;
				case ApiValidationStatus.NeverValidated:
					if (requireValidate)
						throw new JobException(ErrorCode.DreamMakerNeverValidated);
					job.MinimumSecurityLevel = DreamDaemonSecurity.Ultrasafe;
					break;
				case ApiValidationStatus.BadValidationRequest:
				case ApiValidationStatus.Incompatible:
					throw new JobException(ErrorCode.DreamMakerInvalidValidation);
				case ApiValidationStatus.UnaskedValidationRequest:
				default:
					throw new InvalidOperationException(
						$"Session controller returned unexpected ApiValidationStatus: {validationStatus}");
			}
		}

		/// <summary>
		/// Compiles a .dme with DreamMaker.
		/// </summary>
		/// <param name="dreamMakerPath">The path to the DreamMaker executable.</param>
		/// <param name="job">The <see cref="CompileJob"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task<int> RunDreamMaker(string dreamMakerPath, Models.CompileJob job, CancellationToken cancellationToken)
		{
			using var dm = processExecutor.LaunchProcess(
				dreamMakerPath,
				ioManager.ResolvePath(
					job.DirectoryName.ToString()),
				$"-clean {job.DmeName}.{DmeExtension}",
				true,
				true,
				true);

			if (sessionConfiguration.LowPriorityDeploymentProcesses)
				dm.AdjustPriority(false);

			int exitCode;
			using (cancellationToken.Register(() => dm.Terminate()))
				exitCode = await dm.Lifetime;
			cancellationToken.ThrowIfCancellationRequested();

			logger.LogDebug("DreamMaker exit code: {0}", exitCode);
			job.Output = await dm.GetCombinedOutput(cancellationToken);
			currentDreamMakerOutput = job.Output;
			logger.LogDebug("DreamMaker output: {0}{1}", Environment.NewLine, job.Output);
			return exitCode;
		}

		/// <summary>
		/// Adds server side includes to the .dme being compiled.
		/// </summary>
		/// <param name="job">The <see cref="CompileJob"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task ModifyDme(Models.CompileJob job, CancellationToken cancellationToken)
		{
			var dmeFileName = String.Join('.', job.DmeName, DmeExtension);
			var dmePath = ioManager.ConcatPath(job.DirectoryName.ToString(), dmeFileName);
			var dmeReadTask = ioManager.ReadAllBytes(dmePath, cancellationToken);

			var dmeModificationsTask = configuration.CopyDMFilesTo(dmeFileName, ioManager.ResolvePath(job.DirectoryName.ToString()), cancellationToken);

			var dmeBytes = await dmeReadTask;
			var dme = Encoding.UTF8.GetString(dmeBytes);

			var dmeModifications = await dmeModificationsTask;

			if (dmeModifications == null || dmeModifications.TotalDmeOverwrite)
			{
				if (dmeModifications != null)
					logger.LogDebug(".dme replacement configured!");
				else
					logger.LogTrace("No .dme modifications required.");
				return;
			}

			var dmeLines = new List<string>(dme.Split('\n', StringSplitOptions.None));
			for (var dmeLineIndex = 0; dmeLineIndex < dmeLines.Count; ++dmeLineIndex)
			{
				var line = dmeLines[dmeLineIndex];
				if (line.Contains("BEGIN_INCLUDE", StringComparison.Ordinal) && dmeModifications.HeadIncludeLine != null)
				{
					var headIncludeLineNumber = dmeLineIndex + 1;
					logger.LogDebug(
						"Inserting HeadInclude.dm at line {0}: {1}",
						headIncludeLineNumber,
						dmeModifications.HeadIncludeLine);
					dmeLines.Insert(headIncludeLineNumber, dmeModifications.HeadIncludeLine);
					++dmeLineIndex;
				}
				else if (line.Contains("END_INCLUDE", StringComparison.Ordinal) && dmeModifications.TailIncludeLine != null)
				{
					logger.LogDebug(
						"Inserting TailInclude.dm at line {0}: {1}",
						dmeLineIndex,
						dmeModifications.TailIncludeLine);
					dmeLines.Insert(dmeLineIndex, dmeModifications.TailIncludeLine);
					break;
				}
			}

			dmeBytes = Encoding.UTF8.GetBytes(String.Join(Environment.NewLine, dmeLines));
			await ioManager.WriteAllBytes(dmePath, dmeBytes, cancellationToken);
		}

		/// <summary>
		/// Cleans up a failed compile <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The running <see cref="CompileJob"/>.</param>
		/// <param name="remoteDeploymentManager">The <see cref="IRemoteDeploymentManager"/> associated with the <paramref name="job"/>.</param>
		/// <param name="exception">The <see cref="Exception"/> that was thrown.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task CleanupFailedCompile(Models.CompileJob job, IRemoteDeploymentManager remoteDeploymentManager, Exception exception)
		{
			async Task CleanDir()
			{
				logger.LogTrace("Cleaning compile directory...");
				var jobPath = job.DirectoryName.ToString();
				try
				{
					// DCT: None available
					await ioManager.DeleteDirectory(jobPath, default);
				}
				catch (Exception e)
				{
					logger.LogWarning(e, "Error cleaning up compile directory {0}!", ioManager.ResolvePath(jobPath));
				}
			}

			// DCT: None available
			await Task.WhenAll(
				CleanDir(),
				remoteDeploymentManager.FailDeployment(
					job,
					FormatExceptionForUsers(exception),
					default))
				;
		}
	}
}
