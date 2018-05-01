using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace TimeSnapShot
{
    public class PhotoData
    {
        public Guid Id { get; set; }

        public DateTime TimeOccurred { get; set; }

        public string Metadata { get; set; }

       // public byte[]   Image { get; set; }
    }
}
