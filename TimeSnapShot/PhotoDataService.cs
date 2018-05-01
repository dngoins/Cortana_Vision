using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;



namespace TimeSnapShot
{
    public static class PhotoDataService
    {
        public static PhotoData RecordPhotoData(string metadata, byte[] bmp)
        {
            var photoData = new PhotoData
            {
                Id = Guid.NewGuid(),
                Metadata = metadata,
                TimeOccurred = DateTime.UtcNow
            }; //,                Image = bmp             };

            using (var context = new PhotoDataContext())
            {
                context.PhotoDatas.Add(photoData);
                context.SaveChanges();
            }

            return photoData;
        }
        public static IEnumerable<PhotoData> GetRecentPhotoDatas(int numberToRetrieve)
        {
            using (var context = new PhotoDataContext())
            {
                return context.PhotoDatas
                    .OrderByDescending(pd => pd.TimeOccurred)
                    .Take(numberToRetrieve)
                    .ToList();
            }
        }

        public static void ClearHistory()
        {
            using (var context = new PhotoDataContext())
            {
                context.Database.EnsureDeleted();
                context.Database.EnsureCreated();
            }
        }
    }
}
