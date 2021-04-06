using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

namespace chunky
{
    public class CueFile 
    {
        public enum CueType
        {
            File,
            String,
            Virtual
        }

        public readonly ArrayList TrackList = new ArrayList();
        public readonly string BinFileName;
        private readonly string cueFilePath;

        public CueFile(string bin) : this(bin, CueType.Virtual)
        {

        }

        public CueFile(string str, CueType type) 
        {
            Regex trackRegex = new Regex( @"track\s+?(\d+?)\s+?(\S+?)[\s$]+?index\s+?\d+?\s+?(\S*)", RegexOptions.IgnoreCase | RegexOptions.Multiline );
            string cueLines = "  TRACK 01 MODE1/2352\n    INDEX 01 00:00:00";
            if (type == CueType.File)
            {
                this.cueFilePath = str;
                Console.WriteLine("Reading the CUE file:");
                using (TextReader cueReader = new StreamReader(cueFilePath))
                {
                    BinFileName = GetBinFileName(cueReader.ReadLine());
                    cueLines = cueReader.ReadToEnd();
                }
            }
            else if (type == CueType.String)
            {
                using (TextReader cueReader = new StreamReader(cueFilePath))
                {
                    BinFileName = GetBinFileName(cueReader.ReadLine());
                    cueLines = cueReader.ReadToEnd();
                }
                cueLines = str;
            }
            else if (type == CueType.Virtual)
            {
                BinFileName = str;
            }

            var matches = trackRegex.Matches( cueLines );

            if (matches.Count==0)
                throw new ApplicationException( "No tracks was found." );

            Track track = null;
            Track prevTrack = null;
            foreach ( Match trackMatch in matches ) 
            {
                track = new Track( 
                    trackMatch.Groups[1].Value,
                    trackMatch.Groups[2].Value,
                    trackMatch.Groups[3].Value );

                if (chunky.Verbose)
                    Console.WriteLine(" (StartSector {0} ofs {1})", track.StartSector, track.StartPosition);
				
                if (prevTrack != null) 
                {
                    prevTrack.Stop = track.StartPosition - 1;
                    prevTrack.StopSector = track.StartSector;
                }
                TrackList.Add( track );
                prevTrack = track;
            }

            if (track == null) return;
            track.Stop = GetBinFileLength();
            track.StopSector = track.Stop / chunky.SectorLength;
            TrackList[ TrackList.Count -1 ] = track;
        }

        private long GetBinFileLength() 
        {
            var fileInfo = new FileInfo( BinFileName );
            return fileInfo.Length;
        }

        private string GetBinFileName( string cueFirstLine ) 
        {
            Regex binRegex = new Regex( @"file\s+?""(.*?)""", RegexOptions.IgnoreCase | RegexOptions.Multiline );
            Match match = binRegex.Match( cueFirstLine );
            string res = match.Groups[1].Value;
            var cueDirectory = Path.GetDirectoryName( cueFilePath );
            res = Path.Combine( cueDirectory, Path.GetFileName( res ) );
            if (!File.Exists( res ) )
                res = Path.Combine( cueDirectory, Path.GetFileNameWithoutExtension( cueFilePath ) + ".bin" );
            return res;
        }
    }
}