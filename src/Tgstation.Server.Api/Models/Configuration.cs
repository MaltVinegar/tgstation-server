﻿using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a static game file. Create and delete actions uncerimonuously overwrite/delete files
	/// </summary>
#pragma warning disable CA1724 // System.Configuration name conflict
	[Model(RightsType.Configuration, CanCrud = true, CanList = true, RequiresInstance = true, ReadRight = ConfigurationRights.Read, WriteRight = ConfigurationRights.Write)]
	public sealed class Configuration
#pragma warning restore CA1724 // System.Configuration name conflict
	{
		/// <summary>
		/// The path to the <see cref="Configuration"/> file
		/// </summary>
		public string Path { get; set; }

		/// <summary>
		/// If read access to the <see cref="Configuration"/> file was denied
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool ReadDenied { get; set; }

		/// <summary>
		/// If <see cref="Path"/> represents a directory. Will only be <see langword="true"/> if <see cref="ReadDenied"/> is <see langword="true"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool IsDirectory { get; set; }

		/// <summary>
		/// The content of the <see cref="Configuration"/> file. Will be <see langword="null"/> if <see cref="ReadDenied"/> is <see langword="true"/> or during listing operations
		/// </summary>
		public byte[] Content { get; set; }

		/// <summary>
		/// The MD5 hash of the file when last read by the user. Will be <see langword="null"/> if <see cref="ReadDenied"/> is <see langword="true"/>. If this doesn't match during update actions, the write will be denied with error code 409
		/// </summary>
		public string LastReadHash { get; set; }
	}
}
