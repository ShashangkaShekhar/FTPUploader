using System;

namespace FtpTools.Utilities
{
    struct Fileinfo
    {
        public int Id { get; set; }
        public string Filename { get; set; }
        public string Filesize { get; set; }
        public DateTime DateCreated { get; set; }
        public string Filedestination  { get; set; }
        public string Filesource { get; set; }
    }
}
