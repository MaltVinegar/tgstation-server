﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using MySqlConnector;

using Npgsql;

using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Core;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions.Converters;
using Tgstation.Server.Host.IO;
using Tgstation.Server.Host.System;

using YamlDotNet.Serialization;

namespace Tgstation.Server.Host.Setup
{
	/// <inheritdoc />
	sealed class SetupWizard : IHostedService
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IIOManager ioManager;

		/// <summary>
		/// The <see cref="IConsole"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IConsole console;

		/// <summary>
		/// The <see cref="IHostEnvironment"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IHostEnvironment hostingEnvironment;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// The <see cref="IDatabaseConnectionFactory"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IDatabaseConnectionFactory dbConnectionFactory;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="IAsyncDelayer"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IAsyncDelayer asyncDelayer;

		/// <summary>
		/// The <see cref="IHostApplicationLifetime"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly IHostApplicationLifetime applicationLifetime;

		/// <summary>
		/// The <see cref="GeneralConfiguration"/> for the <see cref="SetupWizard"/>.
		/// </summary>
		readonly GeneralConfiguration generalConfiguration;

		/// <summary>
		/// A <see cref="TaskCompletionSource{TResult}"/> that will complete when the <see cref="IConfiguration"/> is reloaded.
		/// </summary>
		TaskCompletionSource<object> reloadTcs;

		/// <summary>
		/// Initializes a new instance of the <see cref="SetupWizard"/> class.
		/// </summary>
		/// <param name="ioManager">The value of <see cref="ioManager"/>.</param>
		/// <param name="console">The value of <see cref="console"/>.</param>
		/// <param name="hostingEnvironment">The value of <see cref="hostingEnvironment"/>.</param>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		/// <param name="dbConnectionFactory">The value of <see cref="dbConnectionFactory"/>.</param>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		/// <param name="asyncDelayer">The value of <see cref="asyncDelayer"/>.</param>
		/// <param name="applicationLifetime">The value of <see cref="applicationLifetime"/>.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> in use.</param>
		/// <param name="generalConfigurationOptions">The <see cref="IOptions{TOptions}"/> containing the value of <see cref="generalConfiguration"/>.</param>
		public SetupWizard(
			IIOManager ioManager,
			IConsole console,
			IHostEnvironment hostingEnvironment,
			IAssemblyInformationProvider assemblyInformationProvider,
			IDatabaseConnectionFactory dbConnectionFactory,
			IPlatformIdentifier platformIdentifier,
			IAsyncDelayer asyncDelayer,
			IHostApplicationLifetime applicationLifetime,
			IConfiguration configuration,
			IOptions<GeneralConfiguration> generalConfigurationOptions)
		{
			this.ioManager = ioManager ?? throw new ArgumentNullException(nameof(ioManager));
			this.console = console ?? throw new ArgumentNullException(nameof(console));
			this.hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
			this.dbConnectionFactory = dbConnectionFactory ?? throw new ArgumentNullException(nameof(dbConnectionFactory));
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));
			this.asyncDelayer = asyncDelayer ?? throw new ArgumentNullException(nameof(asyncDelayer));
			this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
			if (configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			generalConfiguration = generalConfigurationOptions?.Value ?? throw new ArgumentNullException(nameof(generalConfigurationOptions));

			configuration
				.GetReloadToken()
				.RegisterChangeCallback(
					state => reloadTcs?.TrySetResult(null),
					null);
		}

		/// <inheritdoc />
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await CheckRunWizard(cancellationToken);
			applicationLifetime.StopApplication();
		}

		/// <inheritdoc />
		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		/// <summary>
		/// A prompt for a yes or no value.
		/// </summary>
		/// <param name="question">The question <see cref="string"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> resulting in <see langword="true"/> if the user replied yes, <see langword="false"/> otherwise.</returns>
		async Task<bool> PromptYesNo(string question, CancellationToken cancellationToken)
		{
			do
			{
				await console.WriteAsync(question, false, cancellationToken);
				var responseString = await console.ReadLineAsync(false, cancellationToken);
				var upperResponse = responseString.ToUpperInvariant();
				if (upperResponse == "Y" || upperResponse == "YES")
					return true;
				else if (upperResponse == "N" || upperResponse == "NO")
					return false;
				await console.WriteAsync("Invalid response!", true, cancellationToken);
			}
			while (true);
		}

		/// <summary>
		/// Prompts the user to enter the port to host TGS on.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> resulting in the hosting port, or <see langword="null"/> to use the default.</returns>
		async Task<ushort?> PromptForHostingPort(CancellationToken cancellationToken)
		{
			await console.WriteAsync(null, true, cancellationToken);
			await console.WriteAsync("What port would you like to connect to TGS on?", true, cancellationToken);
			await console.WriteAsync("Note: If this is a docker container with the default port already mapped, use the default.", true, cancellationToken);

			do
			{
				await console.WriteAsync(
					$"API Port (leave blank for default of {GeneralConfiguration.DefaultApiPort}): ",
					false,
					cancellationToken)
					;
				var portString = await console.ReadLineAsync(false, cancellationToken);
				if (String.IsNullOrWhiteSpace(portString))
					return null;
				if (UInt16.TryParse(portString, out var port) && port != 0)
					return port;
				await console.WriteAsync("Invalid port! Please enter a value between 1 and 65535", true, cancellationToken);
			}
			while (true);
		}

		/// <summary>
		/// Ensure a given <paramref name="testConnection"/> works.
		/// </summary>
		/// <param name="testConnection">The test <see cref="DbConnection"/>.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/> may have derived data populated.</param>
		/// <param name="databaseName">The database name (or path in the case of a <see cref="DatabaseType.Sqlite"/> database).</param>
		/// <param name="dbExists">Whether or not the database exists.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task TestDatabaseConnection(
			DbConnection testConnection,
			DatabaseConfiguration databaseConfiguration,
			string databaseName,
			bool dbExists,
			CancellationToken cancellationToken)
		{
			bool isSqliteDB = databaseConfiguration.DatabaseType == DatabaseType.Sqlite;
			using (testConnection)
			{
				await console.WriteAsync("Testing connection...", true, cancellationToken);
				await testConnection.OpenAsync(cancellationToken);
				await console.WriteAsync("Connection successful!", true, cancellationToken);

				if (databaseConfiguration.DatabaseType == DatabaseType.MariaDB
					|| databaseConfiguration.DatabaseType == DatabaseType.MySql
					|| databaseConfiguration.DatabaseType == DatabaseType.PostgresSql)
				{
					await console.WriteAsync($"Checking {databaseConfiguration.DatabaseType} version...", true, cancellationToken);
					using var command = testConnection.CreateCommand();
					command.CommandText = "SELECT VERSION()";
					var fullVersion = (string)await command.ExecuteScalarAsync(cancellationToken);
					await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Found {0}", fullVersion), true, cancellationToken);

					if (databaseConfiguration.DatabaseType == DatabaseType.PostgresSql)
					{
						var splits = fullVersion.Split(' ');
						databaseConfiguration.ServerVersion = splits[1].TrimEnd(',');
					}
					else
					{
						var splits = fullVersion.Split('-');
						databaseConfiguration.ServerVersion = splits.First();
					}
				}

				if (!isSqliteDB && !dbExists)
				{
					await console.WriteAsync("Testing create DB permission...", true, cancellationToken);
					using (var command = testConnection.CreateCommand())
					{
						// I really don't care about user sanitization here, they want to fuck their own DB? so be it
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
						command.CommandText = $"CREATE DATABASE {databaseName}";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
						await command.ExecuteNonQueryAsync(cancellationToken);
					}

					await console.WriteAsync("Success!", true, cancellationToken);
					await console.WriteAsync("Dropping test database...", true, cancellationToken);
					using (var command = testConnection.CreateCommand())
					{
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
						command.CommandText = $"DROP DATABASE {databaseName}";
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
						try
						{
							await command.ExecuteNonQueryAsync(cancellationToken);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception e)
						{
							await console.WriteAsync(e.Message, true, cancellationToken);
							await console.WriteAsync(null, true, cancellationToken);
							await console.WriteAsync("This should be okay, but you may want to manually drop the database before continuing!", true, cancellationToken);
							await console.WriteAsync("Press any key to continue...", true, cancellationToken);
							await console.PressAnyKeyAsync(cancellationToken);
						}
					}
				}
			}

			if (isSqliteDB && !dbExists)
				await Task.WhenAll(
					console.WriteAsync("Deleting test database file...", true, cancellationToken),
					ioManager.DeleteFile(databaseName, cancellationToken));
		}

		/// <summary>
		/// Check that a given SQLite <paramref name="databaseName"/> is can be accessed. Also prompts the user if they want to use a relative or absolute path.
		/// </summary>
		/// <param name="databaseName">The path to the potential SQLite database file.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the SQLite database path to store in the configuration.</returns>
		async Task<string> ValidateNonExistantSqliteDBName(string databaseName, CancellationToken cancellationToken)
		{
			var resolvedPath = ioManager.ResolvePath(databaseName);
			try
			{
				var directoryName = ioManager.GetDirectoryName(resolvedPath);
				bool directoryExisted = await ioManager.DirectoryExists(directoryName, cancellationToken);
				await ioManager.CreateDirectory(directoryName, cancellationToken);
				try
				{
					await ioManager.WriteAllBytes(resolvedPath, Array.Empty<byte>(), cancellationToken);
				}
				catch
				{
					if (!directoryExisted)
						await ioManager.DeleteDirectory(directoryName, cancellationToken);
					throw;
				}
			}
			catch (IOException)
			{
				return null;
			}

			if (!Path.IsPathRooted(databaseName))
			{
				await console.WriteAsync("Note, this relative path (currently) resolves to the following:", true, cancellationToken);
				await console.WriteAsync(resolvedPath, true, cancellationToken);
				bool writeResolved = await PromptYesNo(
					"Would you like to save the relative path in the configuration? If not, the full path will be saved. (y/n): ",
					cancellationToken)
					;

				if (writeResolved)
					databaseName = resolvedPath;
			}

			await ioManager.DeleteFile(databaseName, cancellationToken);
			return databaseName;
		}

		/// <summary>
		/// Prompt the user for the <see cref="DatabaseType"/>.
		/// </summary>
		/// <param name="firstTime">If this is the user's first time here.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the input <see cref="DatabaseType"/>.</returns>
		async Task<DatabaseType> PromptDatabaseType(bool firstTime, CancellationToken cancellationToken)
		{
			if (firstTime)
			{
				await console.WriteAsync(String.Empty, true, cancellationToken);
				await console.WriteAsync(
					"NOTE: It is HIGHLY reccommended that TGS runs on a complete relational database, specfically *NOT* Sqlite.",
					true,
					cancellationToken)
					;
				await console.WriteAsync(
					"Sqlite, by nature cannot perform several DDL operations. Because of this future compatiblility cannot be guaranteed.",
					true,
					cancellationToken)
					;
				await console.WriteAsync(
					"This means that you may not be able to update to the next minor version of TGS without a clean re-installation!",
					true,
					cancellationToken)
					;
				await console.WriteAsync(
					"Please consider taking the time to set up a relational database if this is meant to be a long-standing server.",
					true,
					cancellationToken)
					;
				await console.WriteAsync(String.Empty, true, cancellationToken);

				await asyncDelayer.Delay(TimeSpan.FromSeconds(3), cancellationToken);
			}

			await console.WriteAsync("What SQL database type will you be using?", true, cancellationToken);
			do
			{
				await console.WriteAsync(
					String.Format(
						CultureInfo.InvariantCulture,
						"Please enter one of {0}, {1}, {2}, {3} or {4}: ",
						DatabaseType.MariaDB,
						DatabaseType.MySql,
						DatabaseType.PostgresSql,
						DatabaseType.SqlServer,
						DatabaseType.Sqlite),
					false,
					cancellationToken)
					;
				var databaseTypeString = await console.ReadLineAsync(false, cancellationToken);
				if (Enum.TryParse<DatabaseType>(databaseTypeString, out var databaseType))
					return databaseType;

				await console.WriteAsync("Invalid database type!", true, cancellationToken);
			}
			while (true);
		}

		/// <summary>
		/// Prompts the user to create a <see cref="DatabaseConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="DatabaseConfiguration"/>.</returns>
#pragma warning disable CA1502 // TODO: Decomplexify
		async Task<DatabaseConfiguration> ConfigureDatabase(CancellationToken cancellationToken)
		{
			bool firstTime = true;
			do
			{
				await console.WriteAsync(null, true, cancellationToken);

				var databaseConfiguration = new DatabaseConfiguration
				{
					DatabaseType = await PromptDatabaseType(firstTime, cancellationToken),
				};
				firstTime = false;

				string serverAddress = null;
				ushort? serverPort = null;

				bool isSqliteDB = databaseConfiguration.DatabaseType == DatabaseType.Sqlite;
				if (!isSqliteDB)
					do
					{
						await console.WriteAsync(null, true, cancellationToken);
						await console.WriteAsync("Enter the server's address and port [<server>:<port> or <server>] (blank for local): ", false, cancellationToken);
						serverAddress = await console.ReadLineAsync(false, cancellationToken);
						if (String.IsNullOrWhiteSpace(serverAddress))
							serverAddress = null;
						else if (databaseConfiguration.DatabaseType == DatabaseType.SqlServer)
						{
							var match = Regex.Match(serverAddress, @"^(?<server>.+):(?<port>.+)$");
							if (match.Success)
							{
								serverAddress = match.Groups["server"].Value;
								var portString = match.Groups["port"].Value;
								if (UInt16.TryParse(portString, out var port))
									serverPort = port;
								else
								{
									await console.WriteAsync($"Failed to parse port \"{portString}\", please try again.", true, cancellationToken);
									continue;
								}
							}
						}

						break;
					}
					while (true);

				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync($"Enter the database {(isSqliteDB ? "file path" : "name")} (Can be from previous installation. Otherwise, should not exist): ", false, cancellationToken);

				string databaseName;
				bool dbExists = false;
				do
				{
					databaseName = await console.ReadLineAsync(false, cancellationToken);
					if (!String.IsNullOrWhiteSpace(databaseName))
					{
						if (isSqliteDB)
						{
							dbExists = await ioManager.FileExists(databaseName, cancellationToken);
							if (!dbExists)
								databaseName = await ValidateNonExistantSqliteDBName(databaseName, cancellationToken);
						}
						else
							dbExists = await PromptYesNo("Does this database already exist? If not, we will attempt to CREATE it. (y/n): ", cancellationToken);
					}

					if (String.IsNullOrWhiteSpace(databaseName))
						await console.WriteAsync("Invalid database name!", true, cancellationToken);
					else
						break;
				}
				while (true);

				bool useWinAuth;
				if (databaseConfiguration.DatabaseType == DatabaseType.SqlServer && platformIdentifier.IsWindows)
					useWinAuth = await PromptYesNo("Use Windows Authentication? (y/n): ", cancellationToken);
				else
					useWinAuth = false;

				await console.WriteAsync(null, true, cancellationToken);

				string username = null;
				string password = null;
				if (!isSqliteDB)
					if (!useWinAuth)
					{
						await console.WriteAsync("Enter username: ", false, cancellationToken);
						username = await console.ReadLineAsync(false, cancellationToken);
						await console.WriteAsync("Enter password: ", false, cancellationToken);
						password = await console.ReadLineAsync(true, cancellationToken);
					}
					else
					{
						await console.WriteAsync("IMPORTANT: If using the service runner, ensure this computer's LocalSystem account has CREATE DATABASE permissions on the target server!", true, cancellationToken);
						await console.WriteAsync("The account it uses in MSSQL is usually \"NT AUTHORITY\\SYSTEM\" and the role it needs is usually \"dbcreator\".", true, cancellationToken);
						await console.WriteAsync("We'll run a sanity test here, but it won't be indicative of the service's permissions if that is the case", true, cancellationToken);
					}

				await console.WriteAsync(null, true, cancellationToken);

				DbConnection testConnection;
				void CreateTestConnection(string connectionString) =>
					testConnection = dbConnectionFactory.CreateConnection(
						connectionString,
						databaseConfiguration.DatabaseType);

				switch (databaseConfiguration.DatabaseType)
				{
					case DatabaseType.SqlServer:
						{
							var csb = new SqlConnectionStringBuilder
							{
								ApplicationName = assemblyInformationProvider.VersionPrefix,
								DataSource = serverAddress ?? "(local)",
							};

							if (useWinAuth)
								csb.IntegratedSecurity = true;
							else
							{
								csb.UserID = username;
								csb.Password = password;
							}

							CreateTestConnection(csb.ConnectionString);
							csb.InitialCatalog = databaseName;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					case DatabaseType.MariaDB:
					case DatabaseType.MySql:
						{
							// MySQL/MariaDB
							var csb = new MySqlConnectionStringBuilder
							{
								Server = serverAddress ?? "127.0.0.1",
								UserID = username,
								Password = password,
							};

							if (serverPort.HasValue)
								csb.Port = serverPort.Value;

							CreateTestConnection(csb.ConnectionString);
							csb.Database = databaseName;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					case DatabaseType.Sqlite:
						{
							var csb = new SqliteConnectionStringBuilder
							{
								DataSource = databaseName,
								Mode = dbExists ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate,
							};

							CreateTestConnection(csb.ConnectionString);
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					case DatabaseType.PostgresSql:
						{
							var csb = new NpgsqlConnectionStringBuilder
							{
								ApplicationName = assemblyInformationProvider.VersionPrefix,
								Host = serverAddress ?? "127.0.0.1",
								Password = password,
								Username = username,
							};

							if (serverPort.HasValue)
								csb.Port = serverPort.Value;

							CreateTestConnection(csb.ConnectionString);
							csb.Database = databaseName;
							databaseConfiguration.ConnectionString = csb.ConnectionString;
						}

						break;
					default:
						throw new InvalidOperationException("Invalid DatabaseType!");
				}

				try
				{
					await TestDatabaseConnection(testConnection, databaseConfiguration, databaseName, dbExists, cancellationToken);

					return databaseConfiguration;
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception e)
				{
					await console.WriteAsync(e.Message, true, cancellationToken);
					await console.WriteAsync(null, true, cancellationToken);
					await console.WriteAsync("Retrying database configuration...", true, cancellationToken);
				}
			}
			while (true);
		}
#pragma warning restore CA1502

		/// <summary>
		/// Prompts the user to create a <see cref="GeneralConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="GeneralConfiguration"/>.</returns>
		async Task<GeneralConfiguration> ConfigureGeneral(CancellationToken cancellationToken)
		{
			var newGeneralConfiguration = new GeneralConfiguration
			{
				SetupWizardMode = SetupWizardMode.Never,
			};

			do
			{
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Minimum database user password length (leave blank for default of {0}): ", newGeneralConfiguration.MinimumPasswordLength), false, cancellationToken);
				var passwordLengthString = await console.ReadLineAsync(false, cancellationToken);
				if (String.IsNullOrWhiteSpace(passwordLengthString))
					break;
				if (UInt32.TryParse(passwordLengthString, out var passwordLength) && passwordLength >= 0)
				{
					newGeneralConfiguration.MinimumPasswordLength = passwordLength;
					break;
				}

				await console.WriteAsync("Please enter a positive integer!", true, cancellationToken);
			}
			while (true);

			do
			{
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Default timeout for sending and receiving BYOND topics (ms, 0 for infinite, leave blank for default of {0}): ", newGeneralConfiguration.ByondTopicTimeout), false, cancellationToken);
				var topicTimeoutString = await console.ReadLineAsync(false, cancellationToken);
				if (String.IsNullOrWhiteSpace(topicTimeoutString))
					break;
				if (UInt32.TryParse(topicTimeoutString, out var topicTimeout) && topicTimeout >= 0)
				{
					newGeneralConfiguration.ByondTopicTimeout = topicTimeout;
					break;
				}

				await console.WriteAsync("Please enter a positive integer!", true, cancellationToken);
			}
			while (true);

			await console.WriteAsync(null, true, cancellationToken);
			await console.WriteAsync("Enter a GitHub personal access token to bypass some rate limits (this is optional and does not require any scopes)", true, cancellationToken);
			await console.WriteAsync("GitHub personal access token: ", false, cancellationToken);
			newGeneralConfiguration.GitHubAccessToken = await console.ReadLineAsync(true, cancellationToken);
			if (String.IsNullOrWhiteSpace(newGeneralConfiguration.GitHubAccessToken))
				newGeneralConfiguration.GitHubAccessToken = null;

			newGeneralConfiguration.HostApiDocumentation = await PromptYesNo("Host API Documentation? (y/n): ", cancellationToken);

			return newGeneralConfiguration;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="FileLoggingConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="FileLoggingConfiguration"/>.</returns>
		async Task<FileLoggingConfiguration> ConfigureLogging(CancellationToken cancellationToken)
		{
			var fileLoggingConfiguration = new FileLoggingConfiguration();
			await console.WriteAsync(null, true, cancellationToken);
			fileLoggingConfiguration.Disable = !await PromptYesNo("Enable file logging? (y/n): ", cancellationToken);

			if (!fileLoggingConfiguration.Disable)
			{
				do
				{
					await console.WriteAsync("Log file directory path (leave blank for default): ", false, cancellationToken);
					fileLoggingConfiguration.Directory = await console.ReadLineAsync(false, cancellationToken);
					if (String.IsNullOrWhiteSpace(fileLoggingConfiguration.Directory))
					{
						fileLoggingConfiguration.Directory = null;
						break;
					}

					// test a write of it
					await console.WriteAsync(null, true, cancellationToken);
					await console.WriteAsync("Testing directory access...", true, cancellationToken);
					try
					{
						await ioManager.CreateDirectory(fileLoggingConfiguration.Directory, cancellationToken);
						var testFile = ioManager.ConcatPath(fileLoggingConfiguration.Directory, String.Format(CultureInfo.InvariantCulture, "WizardAccesTest.{0}.deleteme", Guid.NewGuid()));
						await ioManager.WriteAllBytes(testFile, Array.Empty<byte>(), cancellationToken);
						try
						{
							await ioManager.DeleteFile(testFile, cancellationToken);
						}
						catch (OperationCanceledException)
						{
							throw;
						}
						catch (Exception e)
						{
							await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Error deleting test log file: {0}", testFile), true, cancellationToken);
							await console.WriteAsync(e.Message, true, cancellationToken);
							await console.WriteAsync(null, true, cancellationToken);
						}

						break;
					}
					catch (OperationCanceledException)
					{
						throw;
					}
					catch (Exception e)
					{
						await console.WriteAsync(e.Message, true, cancellationToken);
						await console.WriteAsync(null, true, cancellationToken);
						await console.WriteAsync("Please verify the path is valid and you have access to it!", true, cancellationToken);
					}
				}
				while (true);

				async Task<LogLevel?> PromptLogLevel(string question)
				{
					do
					{
						await console.WriteAsync(null, true, cancellationToken);
						await console.WriteAsync(question, true, cancellationToken);
						await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Enter one of {0}/{1}/{2}/{3}/{4}/{5} (leave blank for default): ", nameof(LogLevel.Trace), nameof(LogLevel.Debug), nameof(LogLevel.Information), nameof(LogLevel.Warning), nameof(LogLevel.Error), nameof(LogLevel.Critical)), false, cancellationToken);
						var responseString = await console.ReadLineAsync(false, cancellationToken);
						if (String.IsNullOrWhiteSpace(responseString))
							return null;
						if (Enum.TryParse<LogLevel>(responseString, out var logLevel) && logLevel != LogLevel.None)
							return logLevel;
						await console.WriteAsync("Invalid log level!", true, cancellationToken);
					}
					while (true);
				}

				fileLoggingConfiguration.LogLevel = await PromptLogLevel(String.Format(CultureInfo.InvariantCulture, "Enter the level limit for normal logs (default {0}).", fileLoggingConfiguration.LogLevel)) ?? fileLoggingConfiguration.LogLevel;
				fileLoggingConfiguration.MicrosoftLogLevel = await PromptLogLevel(String.Format(CultureInfo.InvariantCulture, "Enter the level limit for Microsoft logs (VERY verbose, default {0}).", fileLoggingConfiguration.MicrosoftLogLevel)) ?? fileLoggingConfiguration.MicrosoftLogLevel;
			}

			return fileLoggingConfiguration;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="ElasticsearchConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ElasticsearchConfiguration"/>.</returns>
		async Task<ElasticsearchConfiguration> ConfigureElasticsearch(CancellationToken cancellationToken)
		{
			var elasticsearchConfiguration = new ElasticsearchConfiguration();
			await console.WriteAsync(null, true, cancellationToken);
			elasticsearchConfiguration.Enable = await PromptYesNo("Enable logging to an external ElasticSearch server? (y/n): ", cancellationToken);

			if (elasticsearchConfiguration.Enable)
			{
				do
				{
					await console.WriteAsync("ElasticSearch server endpoint (Include protocol and port, leave blank for http://127.0.0.1:9200): ", false, cancellationToken);
					elasticsearchConfiguration.Host = await console.ReadLineAsync(false, cancellationToken);
					if (!String.IsNullOrWhiteSpace(elasticsearchConfiguration.Host))
					{
						break;
					}
				}
				while (true);

				do
				{
					await console.WriteAsync("Enter Elasticsearch username: ", false, cancellationToken);
					elasticsearchConfiguration.Username = await console.ReadLineAsync(false, cancellationToken);
					if (!String.IsNullOrWhiteSpace(elasticsearchConfiguration.Username))
					{
						break;
					}
				}
				while (true);

				do
				{
					await console.WriteAsync("Enter password: ", false, cancellationToken);
					elasticsearchConfiguration.Password = await console.ReadLineAsync(true, cancellationToken);
					if (!String.IsNullOrWhiteSpace(elasticsearchConfiguration.Username))
					{
						break;
					}
				}
				while (true);
			}

			return elasticsearchConfiguration;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="ControlPanelConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ControlPanelConfiguration"/>.</returns>
		async Task<ControlPanelConfiguration> ConfigureControlPanel(CancellationToken cancellationToken)
		{
			var config = new ControlPanelConfiguration
			{
				Enable = await PromptYesNo("Enable the web control panel? (y/n): ", cancellationToken),
				AllowAnyOrigin = await PromptYesNo("Allow web control panels hosted elsewhere to access the server? (Access-Control-Allow-Origin: *) (y/n): ", cancellationToken),
			};

			if (!config.AllowAnyOrigin)
			{
				await console.WriteAsync("Enter a comma seperated list of CORS allowed origins (optional): ", false, cancellationToken);
				var commaSeperatedOrigins = await console.ReadLineAsync(false, cancellationToken);
				if (!String.IsNullOrWhiteSpace(commaSeperatedOrigins))
				{
					var splits = commaSeperatedOrigins.Split(',');
					config.AllowedOrigins = new List<string>(splits.Select(x => x.Trim()));
				}
			}

			return config;
		}

		/// <summary>
		/// Prompts the user to create a <see cref="SwarmConfiguration"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="SwarmConfiguration"/>.</returns>
		async Task<SwarmConfiguration> ConfigureSwarm(CancellationToken cancellationToken)
		{
			var enable = await PromptYesNo("Enable swarm mode? (y/n): ", cancellationToken);
			if (!enable)
				return null;

			string identifer;
			do
			{
				await console.WriteAsync("Enter this server's identifer: ", false, cancellationToken);
				identifer = await console.ReadLineAsync(false, cancellationToken);
			}
			while (String.IsNullOrWhiteSpace(identifer));

			async Task<Uri> ParseAddress(string question)
			{
				Uri address;
				do
				{
					await console.WriteAsync(question, false, cancellationToken);
					var addressString = await console.ReadLineAsync(false, cancellationToken);
					if (Uri.TryCreate(addressString, UriKind.Absolute, out address)
						&& address.Scheme != Uri.UriSchemeHttp
						&& address.Scheme != Uri.UriSchemeHttps)
						address = null;
				}
				while (address == null);

				return address;
			}

			var address = await ParseAddress("Enter this server's HTTP(S) address: ");
			string privateKey;
			do
			{
				await console.WriteAsync("Enter the swarm private key: ", false, cancellationToken);
				privateKey = await console.ReadLineAsync(false, cancellationToken);
			}
			while (String.IsNullOrWhiteSpace(privateKey));

			var controller = await PromptYesNo("Is this server the swarm's controller? (y/n): ", cancellationToken);
			Uri controllerAddress = null;
			if (!controller)
				controllerAddress = await ParseAddress("Enter the swarm controller's HTTP(S) address: ");

			return new SwarmConfiguration
			{
				Address = address,
				ControllerAddress = controllerAddress,
				Identifier = identifer,
				PrivateKey = privateKey,
			};
		}

		/// <summary>
		/// Saves a given <see cref="Configuration"/> set to <paramref name="userConfigFileName"/>.
		/// </summary>
		/// <param name="userConfigFileName">The file to save the <see cref="Configuration"/> to.</param>
		/// <param name="hostingPort">The hosting port to save.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/> to save.</param>
		/// <param name="newGeneralConfiguration">The <see cref="GeneralConfiguration"/> to save.</param>
		/// <param name="fileLoggingConfiguration">The <see cref="FileLoggingConfiguration"/> to save.</param>
		/// <param name="elasticsearchConfiguration">The <see cref="ElasticsearchConfiguration"/> to save.</param>
		/// <param name="controlPanelConfiguration">The <see cref="ControlPanelConfiguration"/> to save.</param>
		/// <param name="swarmConfiguration">The <see cref="SwarmConfiguration"/> to save.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task SaveConfiguration(
			string userConfigFileName,
			ushort? hostingPort,
			DatabaseConfiguration databaseConfiguration,
			GeneralConfiguration newGeneralConfiguration,
			FileLoggingConfiguration fileLoggingConfiguration,
			ElasticsearchConfiguration elasticsearchConfiguration,
			ControlPanelConfiguration controlPanelConfiguration,
			SwarmConfiguration swarmConfiguration,
			CancellationToken cancellationToken)
		{
			await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "Configuration complete! Saving to {0}", userConfigFileName), true, cancellationToken);

			newGeneralConfiguration.ApiPort = hostingPort ?? GeneralConfiguration.DefaultApiPort;
			newGeneralConfiguration.ConfigVersion = GeneralConfiguration.CurrentConfigVersion;
			var map = new Dictionary<string, object>()
			{
				{ DatabaseConfiguration.Section, databaseConfiguration },
				{ GeneralConfiguration.Section, newGeneralConfiguration },
				{ FileLoggingConfiguration.Section, fileLoggingConfiguration },
				{ ElasticsearchConfiguration.Section, elasticsearchConfiguration },
				{ ControlPanelConfiguration.Section, controlPanelConfiguration },
				{ SwarmConfiguration.Section, swarmConfiguration },
			};

			var builder = new SerializerBuilder()
				.WithTypeConverter(new VersionConverter());

			if (userConfigFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
				builder.JsonCompatible();

			var serializer = new SerializerBuilder()
				.WithTypeConverter(new VersionConverter())
				.Build();

			var serializedYaml = serializer.Serialize(map);

			// big hack, but, prevent the default control panel channel from being overridden
			serializedYaml = serializedYaml.Replace(
				$"\n  {nameof(ControlPanelConfiguration.Channel)}: ",
				String.Empty,
				StringComparison.Ordinal)
				.Replace("\r", String.Empty, StringComparison.Ordinal);

			var configBytes = Encoding.UTF8.GetBytes(serializedYaml);

			reloadTcs = new TaskCompletionSource<object>();

			try
			{
				await ioManager.WriteAllBytes(userConfigFileName, configBytes, cancellationToken);

				// Ensure the reload
				if (generalConfiguration.SetupWizardMode != SetupWizardMode.Only)
					using (cancellationToken.Register(() => reloadTcs.TrySetCanceled()))
						await reloadTcs.Task;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				await console.WriteAsync(e.Message, true, cancellationToken);
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync("For your convienence, here's the yaml we tried to write out:", true, cancellationToken);
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync(serializedYaml, true, cancellationToken);
				await console.WriteAsync(null, true, cancellationToken);
				await console.WriteAsync("Press any key to exit...", true, cancellationToken);
				await console.PressAnyKeyAsync(cancellationToken);
				throw new OperationCanceledException();
			}
		}

		/// <summary>
		/// Runs the <see cref="SetupWizard"/>.
		/// </summary>
		/// <param name="userConfigFileName">The path to the settings json to build.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task RunWizard(string userConfigFileName, CancellationToken cancellationToken)
		{
			// welcome message
			await console.WriteAsync("Welcome to tgstation-server!", true, cancellationToken);
			await console.WriteAsync("This wizard will help you configure your server.", true, cancellationToken);

			var hostingPort = await PromptForHostingPort(cancellationToken);

			var databaseConfiguration = await ConfigureDatabase(cancellationToken);

			var newGeneralConfiguration = await ConfigureGeneral(cancellationToken);

			var fileLoggingConfiguration = await ConfigureLogging(cancellationToken);

			var elasticSearchConfiguration = await ConfigureElasticsearch(cancellationToken);

			var controlPanelConfiguration = await ConfigureControlPanel(cancellationToken);

			var swarmConfiguration = await ConfigureSwarm(cancellationToken);

			await console.WriteAsync(null, true, cancellationToken);

			await SaveConfiguration(
				userConfigFileName,
				hostingPort,
				databaseConfiguration,
				newGeneralConfiguration,
				fileLoggingConfiguration,
				elasticSearchConfiguration,
				controlPanelConfiguration,
				swarmConfiguration,
				cancellationToken)
				;
		}

		/// <summary>
		/// Check if it should and run the <see cref="SetupWizard"/> if necessary.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		async Task CheckRunWizard(CancellationToken cancellationToken)
		{
			var setupWizardMode = generalConfiguration.SetupWizardMode;
			if (setupWizardMode == SetupWizardMode.Never)
				return;

			var forceRun = setupWizardMode == SetupWizardMode.Force || setupWizardMode == SetupWizardMode.Only;
			if (!console.Available)
			{
				if (forceRun)
					throw new InvalidOperationException("Asked to run setup wizard with no console avaliable!");
				return;
			}

			var userConfigFileName = String.Format(CultureInfo.InvariantCulture, "appsettings.{0}.yml", hostingEnvironment.EnvironmentName);

			async Task HandleSetupCancel()
			{
				// DCTx2: Operation should always run
				await console.WriteAsync(String.Empty, true, default);
				await console.WriteAsync("Aborting setup!", true, default);
			}

			// Link passed cancellationToken with cancel key press
			Task finalTask = Task.CompletedTask;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, console.CancelKeyPress))
			using ((cancellationToken = cts.Token).Register(() => finalTask = HandleSetupCancel()))
				try
				{
					var exists = await ioManager.FileExists(userConfigFileName, cancellationToken);
					if (!exists)
					{
						var legacyJsonFileName = $"appsettings.{hostingEnvironment.EnvironmentName}.json";
						exists = await ioManager.FileExists(legacyJsonFileName, cancellationToken);
						if (exists)
							userConfigFileName = legacyJsonFileName;
					}

					bool shouldRunBasedOnAutodetect;
					if (exists)
					{
						var bytes = await ioManager.ReadAllBytes(userConfigFileName, cancellationToken);
						var contents = Encoding.UTF8.GetString(bytes);
						var existingConfigIsEmpty = String.IsNullOrWhiteSpace(contents) || contents.Trim() == "{}";
						shouldRunBasedOnAutodetect = existingConfigIsEmpty;
					}
					else
						shouldRunBasedOnAutodetect = true;

					if (!shouldRunBasedOnAutodetect)
					{
						if (forceRun)
						{
							await console.WriteAsync(String.Format(CultureInfo.InvariantCulture, "The configuration settings are requesting the setup wizard be run, but you already appear to have a configuration file ({0})!", userConfigFileName), true, cancellationToken);

							forceRun = await PromptYesNo("Continue running setup wizard? (y/n): ", cancellationToken);
						}

						if (!forceRun)
							return;
					}

					// flush the logs to prevent console conflicts
					await asyncDelayer.Delay(TimeSpan.FromSeconds(1), cancellationToken);

					await RunWizard(userConfigFileName, cancellationToken);
				}
				finally
				{
					await finalTask;
				}
		}
	}
}
