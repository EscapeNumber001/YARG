using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using YARG.Data;
using Mackiloha;
using Mackiloha.IO;
using DtxCS;
using DtxCS.DataTypes;

namespace YARG.Serialization {
	public static class RockBandSTFS {
		static RockBandSTFS() {}
		public static List<SongInfo> ParseSongsDta(DirectoryInfo srcfolder) {
			try {
				List<SongInfo> songList = new List<SongInfo>();
				// Encoding dtaEnc = Encoding.GetEncoding("iso-8859-1");
				DataArray asdf = new DataArray();
				using (FileStream str = new FileStream(Path.Combine(srcfolder.FullName, "songs.dta"), FileMode.Open)) {
					asdf = DTX.FromDtaStream(str);
				}
				string dtaFile = asdf.ToString();
				string songPath = "songs/test/test";
				string songPathGen = "songs/" + songPath.Split("/")[1] + "/gen/" + songPath.Split("/")[2];
				var bitmap = serializer.ReadFromFile<HMXBitmap>(Path.Combine(Path.Combine(srcfolder.FullName, songPathGen), "_keep.png_xbox"));
				string tmpFilePath = GetTempFileName() + ".png";
           		bitmap.SaveAs(new SystemInfo(){25, Platform.X360, true}, tmpFilePath);
				return songList;
			} catch (Exception e) {
				Debug.LogError($"Failed to parse songs.dta for `{srcfolder.FullName}`.");
				Debug.LogException(e);
				return null;
			}
		}
	}
}