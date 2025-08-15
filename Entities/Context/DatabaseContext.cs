using Microsoft.EntityFrameworkCore;
using SpotifyBasicApi2025.Entities;

namespace SpotifyBasicApi2025.Entities.Context
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        // Spotify Entities
        public DbSet<SpArtist> SpArtists { get; set; }
        public DbSet<SpAlbum> SpAlbums { get; set; }
        public DbSet<SpTrack> SpTracks { get; set; }
        public DbSet<SpPlaylist> SpPlaylists { get; set; }
        public DbSet<SpUser> SpUsers { get; set; }
        public DbSet<SpSavedTrack> SpSavedTracks { get; set; }
        public DbSet<SpSavedAlbum> SpSavedAlbums { get; set; }
        public DbSet<SpTrackArtist> SpTrackArtists { get; set; }
        public DbSet<SpPlaylistTrack> SpPlaylistTracks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfiguracja relacji many-to-many
            modelBuilder.Entity<SpTrackArtist>()
                .HasOne(ta => ta.Track)
                .WithMany(t => t.TrackArtists)
                .HasForeignKey(ta => ta.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpTrackArtist>()
                .HasOne(ta => ta.Artist)
                .WithMany(a => a.TrackArtists)
                .HasForeignKey(ta => ta.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpPlaylistTrack>()
                .HasOne(pt => pt.Playlist)
                .WithMany(p => p.PlaylistTracks)
                .HasForeignKey(pt => pt.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpPlaylistTrack>()
                .HasOne(pt => pt.Track)
                .WithMany(t => t.PlaylistTracks)
                .HasForeignKey(pt => pt.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja relacji one-to-many
            modelBuilder.Entity<SpAlbum>()
                .HasOne(a => a.Artist)
                .WithMany(ar => ar.Albums)
                .HasForeignKey(a => a.ArtistId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SpTrack>()
                .HasOne(t => t.Album)
                .WithMany(al => al.Tracks)
                .HasForeignKey(t => t.AlbumId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SpPlaylist>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.Playlists)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SpSavedTrack>()
                .HasOne(st => st.User)
                .WithMany(u => u.SavedTracks)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpSavedTrack>()
                .HasOne(st => st.Track)
                .WithMany()
                .HasForeignKey(st => st.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpSavedAlbum>()
                .HasOne(sa => sa.User)
                .WithMany(u => u.SavedAlbums)
                .HasForeignKey(sa => sa.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpSavedAlbum>()
                .HasOne(sa => sa.Album)
                .WithMany()
                .HasForeignKey(sa => sa.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja indeksów dla lepszej wydajności
            modelBuilder.Entity<SpArtist>()
                .HasIndex(a => a.Name);

            modelBuilder.Entity<SpAlbum>()
                .HasIndex(a => a.Name);

            modelBuilder.Entity<SpTrack>()
                .HasIndex(t => t.Name);

            modelBuilder.Entity<SpPlaylist>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<SpUser>()
                .HasIndex(u => u.DisplayName);

            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }
    }
}