using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Windows.Storage;
using System.IO;


namespace TimeSnapShot
{
    public class PhotoDataContext : DbContext
    {
        public DbSet<PhotoData> PhotoDatas { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var dbPath = "PhotoData.db";

            try
            {
                dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbPath);

            }
            catch (Exception ex)
            {

                System.Diagnostics.Trace.WriteLine(ex.Message);

            }
            options.UseSqlite($"Data source={dbPath}");

        }
    }
}
