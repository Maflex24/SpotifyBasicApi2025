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

        // Library Entities
        public DbSet<LibTag> LibTags { get; set; }

        public DbSet<LibTagGroup> LibTagGroups { get; set; }
        public DbSet<LibTagAssign> LibTagAssigns { get; set; }

        // Library connections
        public DbSet<LibArtistUserAssign> LibArtistUserAssigns { get; set; }

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

            // Konfiguracja relacji Library Entities
            modelBuilder.Entity<LibTag>()
                .HasOne(t => t.TagGroup)
                .WithMany(g => g.Tags)
                .HasForeignKey(t => t.TagGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relacje z użytkownikami Spotify
            modelBuilder.Entity<LibTagGroup>()
                .HasOne<SpUser>()
                .WithMany()
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Zmienione z Cascade na NoAction

            // Konfiguracja indeksów dla Library Entities
            modelBuilder.Entity<LibTagGroup>()
                .HasIndex(g => g.Name);

            modelBuilder.Entity<LibTagGroup>()
                .HasIndex(g => g.UserId);

            modelBuilder.Entity<LibTag>()
                .HasIndex(t => t.Name);

            modelBuilder.Entity<LibTag>()
                .HasIndex(t => t.TagGroupId);

            // Konfiguracja relacji LibTagAssign
            modelBuilder.Entity<LibTagAssign>()
                .HasOne(ti => ti.Tag)
                .WithMany()
                .HasForeignKey(ti => ti.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LibTagAssign>()
                .HasOne(ti => ti.User)
                .WithMany()
                .HasForeignKey(ti => ti.UserId)
                .OnDelete(DeleteBehavior.NoAction); // Zmienione z Cascade na NoAction

            // Relacje do elementów Spotify (opcjonalne) - bez kaskadowego usuwania
            modelBuilder.Entity<LibTagAssign>()
                .HasOne(ti => ti.Artist)
                .WithMany()
                .HasForeignKey(ti => ti.SpotifyItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LibTagAssign>()
                .HasOne(ti => ti.Album)
                .WithMany()
                .HasForeignKey(ti => ti.SpotifyItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LibTagAssign>()
                .HasOne(ti => ti.Track)
                .WithMany()
                .HasForeignKey(ti => ti.SpotifyItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LibTagAssign>()
                .HasOne(ti => ti.Playlist)
                .WithMany()
                .HasForeignKey(ti => ti.SpotifyItemId)
                .OnDelete(DeleteBehavior.NoAction);

            // Konfiguracja indeksów dla LibTagAssign
            modelBuilder.Entity<LibTagAssign>()
                .HasIndex(ti => ti.UserId);

            modelBuilder.Entity<LibTagAssign>()
                .HasIndex(ti => ti.TagId);

            modelBuilder.Entity<LibTagAssign>()
                .HasIndex(ti => ti.ItemType);

            modelBuilder.Entity<LibTagAssign>()
                .HasIndex(ti => ti.SpotifyItemId);

            // Unikalny indeks - jeden tag na element dla danego użytkownika
            modelBuilder.Entity<LibTagAssign>()
                .HasIndex(ti => new { ti.UserId, ti.TagId, ti.SpotifyItemId, ti.ItemType })
                .IsUnique();

            // Konfiguracja relacji many-to-many Artist-User
            modelBuilder.Entity<LibArtistUserAssign>()
                .HasOne(aua => aua.User)
                .WithMany(u => u.ArtistAssignments)
                .HasForeignKey(aua => aua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LibArtistUserAssign>()
                .HasOne(aua => aua.Artist)
                .WithMany(a => a.UserAssignments)
                .HasForeignKey(aua => aua.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);

            // Konfiguracja klucza złożonego dla LibArtistUserAssign
            modelBuilder.Entity<LibArtistUserAssign>()
                .HasKey(aua => new { aua.UserId, aua.ArtistId });

            // Indeksy dla LibArtistUserAssign
            modelBuilder.Entity<LibArtistUserAssign>()
                .HasIndex(aua => aua.UserId);

            modelBuilder.Entity<LibArtistUserAssign>()
                .HasIndex(aua => aua.ArtistId);

            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }
    }
}