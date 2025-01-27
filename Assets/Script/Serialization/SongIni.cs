using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using IniParser;
using IniParser.Model;
using UnityEngine;
using YARG.Data;

namespace YARG.Serialization {
	public static class SongIni {
		private static readonly FileIniDataParser PARSER = new();

		static SongIni() {
			PARSER.Parser.Configuration.AllowDuplicateKeys = true;

			// Only match "//" and ";" as comments
			PARSER.Parser.Configuration.CommentRegex = new(@"^//(.*)|^;(.*)");
		}

		public static SongInfo CompleteSongInfo(SongInfo song) {
			if (song.fetched) {
				return song;
			}

			var file = new FileInfo(Path.Combine(song.folder.ToString(), "song.ini"));
			if (!file.Exists) {
				return song;
			}

			song.fetched = true;
			try {
				var data = PARSER.ReadFile(file.FullName, Encoding.UTF8);

				// Get song section name
				KeyDataCollection section;
				if (data.Sections.ContainsSection("song")) {
					section = data["song"];
				} else if (data.Sections.ContainsSection("Song")) {
					section = data["Song"];
				} else {
					Debug.LogError($"No `song` section found in `{song.folder}`.");
					return song;
				}

				// Set basic info
				song.SongName = section["name"];
				song.artistName = section["artist"];

				// Get other metadata
				song.album = section.GetKeyData("album")?.Value;
				song.genre = section.GetKeyData("genre")?.Value;
				song.year = section.GetKeyData("year")?.Value;
				song.loadingPhrase = section.GetKeyData("loading_phrase")?.Value;

				// Get charter
				if (section.ContainsKey("charter")) {
					song.charter = section["charter"];
				} else if (section.ContainsKey("frets")) {
					song.charter = section["frets"];
				}

				// Get song source
				if (section.ContainsKey("icon") && section["icon"] != "0") {
					song.source ??= section["icon"];
				} else {
					song.source = "custom";
				}

				// Get song length
				if (section.ContainsKey("song_length")) {
					int rawLength = int.Parse(section["song_length"]);
					song.songLength = rawLength / 1000f;
				} else {
					Debug.LogWarning($"No song length found for `{song.folder}`. Loading audio file. This might take longer.");
					LoadSongLengthFromAudio(song);
				}

				// Get drum type
				if (section.ContainsKey("pro_drums") && (
					section["pro_drums"].ToLowerInvariant() == "true" ||
					section["pro_drums"] == "1")) {

					song.drumType = SongInfo.DrumType.FOUR_LANE;
				} else if (section.ContainsKey("five_lane_drums") && (
					section["five_lane_drums"].ToLowerInvariant() == "true" ||
					section["five_lane_drums"] == "1")) {

					song.drumType = SongInfo.DrumType.FIVE_LANE;
				} else {
					song.drumType = SongInfo.DrumType.UNKNOWN;
				}

				// Get song delay (0 if none)
				if (section.ContainsKey("delay")) {
					int rawDelay = int.Parse(section["delay"]);
					song.delay = rawDelay / 1000f;
				} else {
					song.delay = 0f;
				}

				// Get hopo frequency
				// Standardized here: https://github.com/TheNathannator/GuitarGame_ChartFormats/blob/main/doc/FileFormats/.mid/Standard/5-Fret%20Guitar.md#note-mechanics
				if (section.ContainsKey("hopo_frequency")) {
					song.hopoFreq = int.Parse(section["hopo_frequency"]);
				} else if (section.ContainsKey("hopofreq")) {
					song.hopoFreq = int.Parse(section["hopofreq"]);
				} else if (section.ContainsKey("eighthnote_hopo")) {
					if (section["eighthnote_hopo"].ToLowerInvariant() == "true" ||
						section["eighthnote_hopo"] == "1") {

						song.hopoFreq = 240;
					}
				}

				// Get difficulties
				bool noneFound = true;
				foreach (var kvp in new Dictionary<string, int>(song.partDifficulties)) {
					var key = "diff_" + (kvp.Key switch {
						"realGuitar" => "guitar_real",
						"realBass" => "bass_real",
						"realDrums" => "drums_real",
						"realKeys" => "keys_real",
						"harmVocals" => "vocals_harm",
						_ => kvp.Key
					});

					if (section.ContainsKey(key)) {
						song.partDifficulties[kvp.Key] = int.Parse(section[key]);
						noneFound = true;
					}
				}

				// If no difficulties found, check the source
				if (noneFound) {
					if (song.source == "gh1") {
						song.partDifficulties["guitar"] = -2;
					} else if (song.source == "gh2"
						|| song.source == "gh80s"
						|| song.source == "gh3"
						|| song.source == "ghot"
						|| song.source == "gha") {

						song.partDifficulties["guitar"] = -2;
						song.partDifficulties["bass"] = -2;
					}
				}
			} catch (Exception e) {
				Debug.LogError($"Failed to parse song.ini for `{song.folder}`.");
				Debug.LogException(e);
			}

			return song;
		}

		private static void LoadSongLengthFromAudio(SongInfo song) {
			// Load file
			var songOggPath = Path.Combine(song.folder.FullName, "song.ogg");
			var file = TagLib.File.Create(songOggPath);

			// Save 
			song.songLength = (float) file.Properties.Duration.TotalSeconds;
		}
	}
}