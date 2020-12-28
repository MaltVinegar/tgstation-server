// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tgstation.Server.Host.Database.Migrations
{
	[DbContext(typeof(MySqlDatabaseContext))]
	partial class MySqlDatabaseContextModelSnapshot : ModelSnapshot
	{
		/// <inheritdoc />
		protected override void BuildModel(ModelBuilder modelBuilder)
		{
#pragma warning disable 612, 618
			modelBuilder
				.HasAnnotation("ProductVersion", "3.1.10")
				.HasAnnotation("Relational:MaxIdentifierLength", 64);

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<ushort?>("ChannelLimit")
					.IsRequired()
					.HasColumnType("smallint unsigned");

				b.Property<string>("ConnectionString")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<bool?>("Enabled")
					.HasColumnType("tinyint(1)");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<string>("Name")
					.IsRequired()
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.Property<int>("Provider")
					.HasColumnType("int");

				b.Property<uint?>("ReconnectionInterval")
					.IsRequired()
					.HasColumnType("int unsigned");

				b.HasKey("Id");

				b.HasIndex("InstanceId", "Name")
					.IsUnique();

				b.ToTable("ChatBots");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<long>("ChatSettingsId")
					.HasColumnType("bigint");

				b.Property<ulong?>("DiscordChannelId")
					.HasColumnType("bigint unsigned");

				b.Property<string>("IrcChannel")
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.Property<bool?>("IsAdminChannel")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<bool?>("IsUpdatesChannel")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<bool?>("IsWatchdogChannel")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<string>("Tag")
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.HasKey("Id");

				b.HasIndex("ChatSettingsId", "DiscordChannelId")
					.IsUnique();

				b.HasIndex("ChatSettingsId", "IrcChannel")
					.IsUnique();

				b.ToTable("ChatChannels");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("ByondVersion")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<int?>("DMApiMajorVersion")
					.HasColumnType("int");

				b.Property<int?>("DMApiMinorVersion")
					.HasColumnType("int");

				b.Property<int?>("DMApiPatchVersion")
					.HasColumnType("int");

				b.Property<Guid?>("DirectoryName")
					.IsRequired()
					.HasColumnType("char(36)");

				b.Property<string>("DmeName")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<int?>("GitHubDeploymentId")
					.HasColumnType("int");

				b.Property<long?>("GitHubRepoId")
					.HasColumnType("bigint");

				b.Property<long>("JobId")
					.HasColumnType("bigint");

				b.Property<int?>("MinimumSecurityLevel")
					.HasColumnType("int");

				b.Property<string>("Output")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<string>("RepositoryOrigin")
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<long>("RevisionInformationId")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("DirectoryName");

				b.HasIndex("JobId")
					.IsUnique();

				b.HasIndex("RevisionInformationId");

				b.ToTable("CompileJobs");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamDaemonSettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("AdditionalParameters")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<bool?>("AllowWebClient")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<bool?>("AutoStart")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<uint?>("HeartbeatSeconds")
					.IsRequired()
					.HasColumnType("int unsigned");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<ushort?>("Port")
					.IsRequired()
					.HasColumnType("smallint unsigned");

				b.Property<int>("SecurityLevel")
					.HasColumnType("int");

				b.Property<uint?>("StartupTimeout")
					.IsRequired()
					.HasColumnType("int unsigned");

				b.Property<uint?>("TopicRequestTimeout")
					.IsRequired()
					.HasColumnType("int unsigned");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("DreamDaemonSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<ushort?>("ApiValidationPort")
					.IsRequired()
					.HasColumnType("smallint unsigned");

				b.Property<int>("ApiValidationSecurityLevel")
					.HasColumnType("int");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<string>("ProjectName")
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<bool?>("RequireDMApiValidation")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("DreamMakerSettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Instance", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<uint?>("AutoUpdateInterval")
					.IsRequired()
					.HasColumnType("int unsigned");

				b.Property<ushort?>("ChatBotLimit")
					.IsRequired()
					.HasColumnType("smallint unsigned");

				b.Property<int>("ConfigurationType")
					.HasColumnType("int");

				b.Property<string>("Name")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<bool?>("Online")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<string>("Path")
					.IsRequired()
					.HasColumnType("varchar(255) CHARACTER SET utf8mb4");

				b.Property<string>("SwarmIdentifer")
					.HasColumnType("varchar(255) CHARACTER SET utf8mb4");

				b.HasKey("Id");

				b.HasIndex("Path", "SwarmIdentifer")
					.IsUnique();

				b.ToTable("Instances");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstancePermissionSet", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<ulong>("ByondRights")
					.HasColumnType("bigint unsigned");

				b.Property<ulong>("ChatBotRights")
					.HasColumnType("bigint unsigned");

				b.Property<ulong>("ConfigurationRights")
					.HasColumnType("bigint unsigned");

				b.Property<ulong>("DreamDaemonRights")
					.HasColumnType("bigint unsigned");

				b.Property<ulong>("DreamMakerRights")
					.HasColumnType("bigint unsigned");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<ulong>("InstancePermissionSetRights")
					.HasColumnType("bigint unsigned");

				b.Property<long>("PermissionSetId")
					.HasColumnType("bigint");

				b.Property<ulong>("RepositoryRights")
					.HasColumnType("bigint unsigned");

				b.HasKey("Id");

				b.HasIndex("InstanceId");

				b.HasIndex("PermissionSetId", "InstanceId")
					.IsUnique();

				b.ToTable("InstancePermissionSets");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Job", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<ulong?>("CancelRight")
					.HasColumnType("bigint unsigned");

				b.Property<ulong?>("CancelRightsType")
					.HasColumnType("bigint unsigned");

				b.Property<bool?>("Cancelled")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<long?>("CancelledById")
					.HasColumnType("bigint");

				b.Property<string>("Description")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<uint?>("ErrorCode")
					.HasColumnType("int unsigned");

				b.Property<string>("ExceptionDetails")
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<DateTimeOffset?>("StartedAt")
					.IsRequired()
					.HasColumnType("datetime(6)");

				b.Property<long>("StartedById")
					.HasColumnType("bigint");

				b.Property<DateTimeOffset?>("StoppedAt")
					.HasColumnType("datetime(6)");

				b.HasKey("Id");

				b.HasIndex("CancelledById");

				b.HasIndex("InstanceId");

				b.HasIndex("StartedById");

				b.ToTable("Jobs");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.OAuthConnection", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("ExternalUserId")
					.IsRequired()
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.Property<int>("Provider")
					.HasColumnType("int");

				b.Property<long?>("UserId")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("UserId");

				b.HasIndex("Provider", "ExternalUserId")
					.IsUnique();

				b.ToTable("OAuthConnections");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.PermissionSet", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<ulong>("AdministrationRights")
					.HasColumnType("bigint unsigned");

				b.Property<long?>("GroupId")
					.HasColumnType("bigint");

				b.Property<ulong>("InstanceManagerRights")
					.HasColumnType("bigint unsigned");

				b.Property<long?>("UserId")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("GroupId")
					.IsUnique();

				b.HasIndex("UserId")
					.IsUnique();

				b.ToTable("PermissionSets");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("AccessIdentifier")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<long>("CompileJobId")
					.HasColumnType("bigint");

				b.Property<int>("LaunchSecurityLevel")
					.HasColumnType("int");

				b.Property<ushort>("Port")
					.HasColumnType("smallint unsigned");

				b.Property<int>("ProcessId")
					.HasColumnType("int");

				b.Property<int>("RebootState")
					.HasColumnType("int");

				b.HasKey("Id");

				b.HasIndex("CompileJobId");

				b.ToTable("ReattachInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("AccessToken")
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<string>("AccessUser")
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<bool?>("AutoUpdatesKeepTestMerges")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<bool?>("AutoUpdatesSynchronize")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<string>("CommitterEmail")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<string>("CommitterName")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<bool?>("CreateGitHubDeployments")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<bool?>("PostTestMergeComment")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<bool?>("PushTestMergeCommits")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<bool?>("ShowTestMergeCommitters")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.HasKey("Id");

				b.HasIndex("InstanceId")
					.IsUnique();

				b.ToTable("RepositorySettings");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<long>("RevisionInformationId")
					.HasColumnType("bigint");

				b.Property<long>("TestMergeId")
					.HasColumnType("bigint");

				b.HasKey("Id");

				b.HasIndex("RevisionInformationId");

				b.HasIndex("TestMergeId");

				b.ToTable("RevInfoTestMerges");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("CommitSha")
					.IsRequired()
					.HasColumnType("varchar(40) CHARACTER SET utf8mb4")
					.HasMaxLength(40);

				b.Property<long>("InstanceId")
					.HasColumnType("bigint");

				b.Property<string>("OriginCommitSha")
					.IsRequired()
					.HasColumnType("varchar(40) CHARACTER SET utf8mb4")
					.HasMaxLength(40);

				b.HasKey("Id");

				b.HasIndex("InstanceId", "CommitSha")
					.IsUnique();

				b.ToTable("RevisionInformations");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("Author")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<string>("BodyAtMerge")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<string>("Comment")
					.HasColumnType("longtext CHARACTER SET utf8mb4")
					.HasMaxLength(10000);

				b.Property<DateTimeOffset>("MergedAt")
					.HasColumnType("datetime(6)");

				b.Property<long>("MergedById")
					.HasColumnType("bigint");

				b.Property<int>("Number")
					.HasColumnType("int");

				b.Property<long?>("PrimaryRevisionInformationId")
					.IsRequired()
					.HasColumnType("bigint");

				b.Property<string>("TargetCommitSha")
					.IsRequired()
					.HasColumnType("varchar(40) CHARACTER SET utf8mb4")
					.HasMaxLength(40);

				b.Property<string>("TitleAtMerge")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<string>("Url")
					.IsRequired()
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.HasKey("Id");

				b.HasIndex("MergedById");

				b.HasIndex("PrimaryRevisionInformationId")
					.IsUnique();

				b.ToTable("TestMerges");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
			{
				b.Property<long?>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("CanonicalName")
					.IsRequired()
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.Property<DateTimeOffset?>("CreatedAt")
					.IsRequired()
					.HasColumnType("datetime(6)");

				b.Property<long?>("CreatedById")
					.HasColumnType("bigint");

				b.Property<bool?>("Enabled")
					.IsRequired()
					.HasColumnType("tinyint(1)");

				b.Property<long?>("GroupId")
					.HasColumnType("bigint");

				b.Property<DateTimeOffset?>("LastPasswordUpdate")
					.HasColumnType("datetime(6)");

				b.Property<string>("Name")
					.IsRequired()
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.Property<string>("PasswordHash")
					.HasColumnType("longtext CHARACTER SET utf8mb4");

				b.Property<string>("SystemIdentifier")
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.HasKey("Id");

				b.HasIndex("CanonicalName")
					.IsUnique();

				b.HasIndex("CreatedById");

				b.HasIndex("GroupId");

				b.HasIndex("SystemIdentifier")
					.IsUnique();

				b.ToTable("Users");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.UserGroup", b =>
			{
				b.Property<long>("Id")
					.ValueGeneratedOnAdd()
					.HasColumnType("bigint");

				b.Property<string>("Name")
					.IsRequired()
					.HasColumnType("varchar(100) CHARACTER SET utf8mb4")
					.HasMaxLength(100);

				b.HasKey("Id");

				b.HasIndex("Name")
					.IsUnique();

				b.ToTable("Groups");
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatBot", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("ChatSettings")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ChatChannel", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.ChatBot", "ChatSettings")
					.WithMany("Channels")
					.HasForeignKey("ChatSettingsId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.CompileJob", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Job", "Job")
					.WithOne()
					.HasForeignKey("Tgstation.Server.Host.Models.CompileJob", "JobId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "RevisionInformation")
					.WithMany("CompileJobs")
					.HasForeignKey("RevisionInformationId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamDaemonSettings", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithOne("DreamDaemonSettings")
					.HasForeignKey("Tgstation.Server.Host.Models.DreamDaemonSettings", "InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.DreamMakerSettings", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithOne("DreamMakerSettings")
					.HasForeignKey("Tgstation.Server.Host.Models.DreamMakerSettings", "InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.InstancePermissionSet", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("InstancePermissionSets")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.PermissionSet", "PermissionSet")
					.WithMany("InstancePermissionSets")
					.HasForeignKey("PermissionSetId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.Job", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "CancelledBy")
					.WithMany()
					.HasForeignKey("CancelledById");

				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("Jobs")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.User", "StartedBy")
					.WithMany()
					.HasForeignKey("StartedById")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.OAuthConnection", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "User")
					.WithMany("OAuthConnections")
					.HasForeignKey("UserId")
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.PermissionSet", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.UserGroup", "Group")
					.WithOne("PermissionSet")
					.HasForeignKey("Tgstation.Server.Host.Models.PermissionSet", "GroupId")
					.OnDelete(DeleteBehavior.Cascade);

				b.HasOne("Tgstation.Server.Host.Models.User", "User")
					.WithOne("PermissionSet")
					.HasForeignKey("Tgstation.Server.Host.Models.PermissionSet", "UserId")
					.OnDelete(DeleteBehavior.Cascade);
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.ReattachInformation", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.CompileJob", "CompileJob")
					.WithMany()
					.HasForeignKey("CompileJobId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RepositorySettings", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithOne("RepositorySettings")
					.HasForeignKey("Tgstation.Server.Host.Models.RepositorySettings", "InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevInfoTestMerge", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "RevisionInformation")
					.WithMany("ActiveTestMerges")
					.HasForeignKey("RevisionInformationId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.TestMerge", "TestMerge")
					.WithMany("RevisonInformations")
					.HasForeignKey("TestMergeId")
					.OnDelete(DeleteBehavior.ClientNoAction)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.RevisionInformation", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.Instance", "Instance")
					.WithMany("RevisionInformations")
					.HasForeignKey("InstanceId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.TestMerge", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "MergedBy")
					.WithMany("TestMerges")
					.HasForeignKey("MergedById")
					.OnDelete(DeleteBehavior.Restrict)
					.IsRequired();

				b.HasOne("Tgstation.Server.Host.Models.RevisionInformation", "PrimaryRevisionInformation")
					.WithOne("PrimaryTestMerge")
					.HasForeignKey("Tgstation.Server.Host.Models.TestMerge", "PrimaryRevisionInformationId")
					.OnDelete(DeleteBehavior.Cascade)
					.IsRequired();
			});

			modelBuilder.Entity("Tgstation.Server.Host.Models.User", b =>
			{
				b.HasOne("Tgstation.Server.Host.Models.User", "CreatedBy")
					.WithMany("CreatedUsers")
					.HasForeignKey("CreatedById");

				b.HasOne("Tgstation.Server.Host.Models.UserGroup", "Group")
					.WithMany("Users")
					.HasForeignKey("GroupId");
			});
#pragma warning restore 612, 618
		}
	}
}
