using ExamScheduler.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExamScheduler.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Bolum> Bolumler { get; set; }
        public DbSet<Ders> Dersler { get; set; }
        public DbSet<Oturum> Oturumlar { get; set; }
        public DbSet<Derslik> Derslikler { get; set; }
        public DbSet<Personel> Personeller { get; set; }
        public DbSet<PersonelDurum> PersonelDurumlar { get; set; }
        public DbSet<Sinav> Sinavlar { get; set; }
        public DbSet<SinavSalonu> SinavSalonlari { get; set; }
        public DbSet<GozetmenAtama> GozetmenAtamalari { get; set; }
        public DbSet<IslemLog> IslemLoglari { get; set; }
        public DbSet<Kullanici> Kullanicilar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // İlişkiler ve Kısıtlamalar (Fluent API)
            modelBuilder.Entity<Ders>()
                .HasOne(d => d.Bolum)
                .WithMany(b => b.Dersler)
                .HasForeignKey(d => d.BolumId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Personel>()
                .HasOne(p => p.Bolum)
                .WithMany(b => b.Personeller)
                .HasForeignKey(p => p.BolumId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Sinav>()
                .HasOne(s => s.Ders)
                .WithMany(d => d.Sinavlar)
                .HasForeignKey(s => s.DersId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Sinav>()
                .HasOne(s => s.Oturum)
                .WithMany(o => o.Sinavlar)
                .HasForeignKey(s => s.OturumId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SinavSalonu>()
                .HasOne(ss => ss.Sinav)
                .WithMany(s => s.Salonlar)
                .HasForeignKey(ss => ss.SinavId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SinavSalonu>()
                .HasOne(ss => ss.Derslik)
                .WithMany(d => d.SinavSalonlari)
                .HasForeignKey(ss => ss.DerslikId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GozetmenAtama>()
                .HasOne(ga => ga.SinavSalonu)
                .WithMany(ss => ss.Gozetmenler)
                .HasForeignKey(ga => ga.SinavSalonuId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GozetmenAtama>()
                .HasOne(ga => ga.Personel)
                .WithMany(p => p.Atamalar)
                .HasForeignKey(ga => ga.PersonelId)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<PersonelDurum>()
                .HasOne(pd => pd.Personel)
                .WithMany(p => p.Durumlar)
                .HasForeignKey(pd => pd.PersonelId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
