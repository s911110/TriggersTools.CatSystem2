﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using TriggersTools.CatSystem2.Structs;
using TriggersTools.CatSystem2.Utils;
using TriggersTools.SharpUtils.Exceptions;
using TriggersTools.SharpUtils.IO;

namespace TriggersTools.CatSystem2 {
	partial class CstScene {
		#region Extract

		/// <summary>
		///  Extracts the scene script information.
		/// </summary>
		/// <param name="cstFile">The file path to the CST scene script file to extract.</param>
		/// <returns>The extracted <see cref="CstScene"/> information.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		///  <paramref name="cstFile"/> is null.
		/// </exception>
		public static CstScene Extract(string cstFile) {
			using (var stream = File.OpenRead(cstFile))
				return Extract(stream, cstFile);
		}
		/// <summary>
		///  Extracts the scene script information.
		/// </summary>
		/// <param name="stream">The stream to extract the scene script from.</param>
		/// <param name="fileName">The path or name of the scene script file being extracted.</param>
		/// <returns>The extracted <see cref="CstScene"/> information.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		///  <paramref name="stream"/> or <paramref name="fileName"/> is null.
		/// </exception>
		/// <exception cref="ObjectDisposedException">
		///  The <paramref name="stream"/> is closed.
		/// </exception>
		public static CstScene Extract(Stream stream, string fileName) {
			BinaryReader reader = new BinaryReader(stream);
			CATSCENEHDR hdr = reader.ReadUnmanaged<CATSCENEHDR>();

			UnexpectedFileTypeException.ThrowIfInvalid(hdr.Signature, CATSCENEHDR.ExpectedSignature);

			byte[] scriptData = Zlib.Decompress(reader, hdr.CompressedLength, hdr.DecompressedLength);
			/*byte[] compressed = reader.ReadBytes(hdr.CompressedLength);
			byte[] decompressed = new byte[hdr.DecompressedLength];
			int decompressedLength = hdr.DecompressedLength;
			Zlib.Uncompress(decompressed, ref decompressedLength, compressed, hdr.CompressedLength);*/

			SCRIPTLINE[] lines;

			using (var ms = new MemoryStream(scriptData))
				lines = ReadScript(ms);

			return new CstScene(fileName, lines);
		}

		#endregion

		#region ReadScript

		/// <summary>
		///  Reads the scene line entries in the script.
		/// </summary>
		/// <param name="stream">The stream to read the script from.</param>
		/// <param name="length">The actual length of the script file.</param>
		/// <returns>The scene line entries.</returns>
		private static SCRIPTLINE[] ReadScript(Stream stream) {
			BinaryReader reader = new BinaryReader(stream, CatUtils.ShiftJIS);
			SCRIPTHDR hdr = reader.ReadUnmanaged<SCRIPTHDR>();

			if (hdr.ScriptLength + SCRIPTHDR.CbSize != stream.Length)
				throw new Exception("Corrupted Script!");
			
			int entryCount = hdr.EntryCount;
			
			// These values are only needed at runtime to speed things up.
			// They can be calulcated protgrammatically.
			//SCRIPTINPUT[] inputs = reader.ReadUnmanagedArray<SCRIPTINPUT>(hdr.InputCount);

			// We technically should already be at this position
			stream.Position = hdr.OffsetTable + SCRIPTHDR.CbSize;

			int[] offsets = reader.ReadInt32s(entryCount);
			SCRIPTLINE[] entries = new SCRIPTLINE[entryCount];

			for (int i = 0; i < entryCount; i++) {
				stream.Position = offsets[i] + hdr.StringTable + SCRIPTHDR.CbSize;
				entries[i] = new SCRIPTLINE {
					Type = reader.ReadUInt16(),
					Content = reader.ReadTerminatedString(),
				};
			}

			// Validation to make sure I understand the SCRIPTINPUT structures correctly
			// The goal is to be able to programmatically generate these files
			/*if ((entries[entryCount - 1].Type & 0x200) != 0) {
				if (entries.Where(e => (e.Type & 0x200) != 0).Count() != hdr.InputCount)
					throw new Exception();
			}
			else if (entries.Where(e => (e.Type & 0x200) != 0).Count() + 1 != hdr.InputCount)
				throw new Exception();

			SCRIPTINPUT input = inputs[0];
			if (input.Index != 0)
				throw new Exception();
			for (int i = 1; i < hdr.InputCount; i++) {
				input.Index += input.OffsetNext;
				SCRIPTINPUT nextInput = inputs[i];
				if ((entries[input.Index - 1].Type & 0x200) == 0)
					throw new Exception();
				if (nextInput.Index != input.Index)
					throw new Exception();
				input = nextInput;
			}
			input.Index += input.OffsetNext;
			if (input.Index != entryCount)
				throw new Exception();*/

			return entries;
		}

		#endregion
	}
	partial class KifintEntryExtensions {
		#region ExtractScene

		/// <summary>
		///  Extracts the CST scene script from the entry.
		/// </summary>
		/// <param name="entry">The entry to extract from.</param>
		/// <returns>The extracted scene script.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		///  <paramref name="entry"/> is null.
		/// </exception>
		public static CstScene ExtractScene(this KifintEntry entry) {
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			using (var stream = entry.ExtractToStream())
				return CstScene.Extract(stream, entry.FileName);
		}
		/// <summary>
		///  Extracts the CST scene script from the open KIFINT archive stream.
		/// </summary>
		/// <param name="entry">The entry to extract from.</param>
		/// <param name="kifintStream">The stream to the open KIFINT archive.</param>
		/// <returns>The extracted scene script.</returns>
		/// 
		/// <exception cref="ArgumentNullException">
		///  <paramref name="entry"/> or <paramref name="kifintStream"/> is null.
		/// </exception>
		public static CstScene ExtractScene(this KifintEntry entry, KifintStream kifintStream) {
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			using (var stream = entry.ExtractToStream(kifintStream))
				return CstScene.Extract(stream, entry.FileName);
		}

		#endregion
	}
}
