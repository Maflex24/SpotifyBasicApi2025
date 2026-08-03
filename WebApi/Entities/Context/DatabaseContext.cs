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
        public DbSet<SpAlbumArtistAssign> SpAlbumArtistAssigns { get; set; }

        // Library Entities
        public DbSet<LibTag> LibTags { get; set; }

        public DbSet<LibTagGroup> LibTagGroups { get; set; }
        public DbSet<LibTagAssign> LibTagAssigns { get; set; }
        public DbSet<LibPlaylistFolder> LibPlaylistFolders { get; set; }
        public DbSet<LibPlaylistFolderAssign> LibPlaylistFolderAssigns { get; set; }

        // Library connections
        public DbSet<LibArtistUserAssign> LibArtistUserAssigns { get; set; }

        public DbSet<LibAlbumUserAssign> LibAlbumUserAssigns { get; set; }

        public DbSet<LibTrackUserAssign> LibTrackUserAssigns { get; set; }
        public DbSet<LibSavedFilter> LibSavedFilters { get; set; }

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
            modelBuilder.Entity<SpTrack>()
                .HasOne(t => t.Album)
                .WithMany(al => al.Tracks)
                .HasForeignKey(t => t.AlbumId)
                .OnDelete(DeleteBehavior.SetNull);

            // Konfiguracja relacji many-to-many Album-Artist
            modelBuilder.Entity<SpAlbumArtistAssign>()
                .HasOne(aaa => aaa.Album)
                .WithMany(a => a.AlbumArtistAssigns)
                .HasForeignKey(aaa => aaa.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpAlbumArtistAssign>()
                .HasOne(aaa => aaa.Artist)
                .WithMany(a => a.AlbumArtistAssigns)
                .HasForeignKey(aaa => aaa.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indeksy dla SpAlbumArtistAssign
            modelBuilder.Entity<SpAlbumArtistAssign>()
                .HasIndex(aaa => aaa.AlbumId);

            modelBuilder.Entity<SpAlbumArtistAssign>()
                .HasIndex(aaa => aaa.ArtistId);

            // Unikalny indeks - jeden artysta na album
            modelBuilder.Entity<SpAlbumArtistAssign>()
                .HasIndex(aaa => new { aaa.AlbumId, aaa.ArtistId })
                .IsUnique();

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
            // Playlist folders
            modelBuilder.Entity<LibPlaylistFolder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LibPlaylistFolder>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<SpPlaylist>()
                .HasOne(p => p.PlaylistFolder)
                .WithMany(f => f.Playlists)
                .HasForeignKey(p => p.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Assign: User + Playlist -> Folder (opcjonalny)
            modelBuilder.Entity<LibPlaylistFolderAssign>()
                .HasIndex(a => new { a.UserId, a.PlaylistId })
                .IsUnique();

            modelBuilder.Entity<LibPlaylistFolderAssign>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<LibPlaylistFolderAssign>()
                .HasOne(a => a.Playlist)
                .WithMany()
                .HasForeignKey(a => a.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LibPlaylistFolderAssign>()
                .HasOne(a => a.Folder)
                .WithMany()
                .HasForeignKey(a => a.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

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

            // Usuwamy twarde FK do tabel Spotify dla LibTagAssign (połączenie miękkie po ItemType + SpotifyItemId)

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

            // Indeksy dla LibArtistUserAssign
            modelBuilder.Entity<LibArtistUserAssign>()
                .HasIndex(aua => aua.UserId);

            modelBuilder.Entity<LibArtistUserAssign>()
                .HasIndex(aua => aua.ArtistId);

            // Unikalny indeks - jeden artysta na użytkownika
            modelBuilder.Entity<LibArtistUserAssign>()
                .HasIndex(aua => new { aua.UserId, aua.ArtistId })
                .IsUnique();

            // Konfiguracja relacji many-to-many Album-User
            modelBuilder.Entity<LibAlbumUserAssign>()
                .HasOne(aua => aua.User)
                .WithMany(u => u.AlbumAssignments)
                .HasForeignKey(aua => aua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LibAlbumUserAssign>()
                .HasOne(aua => aua.Album)
                .WithMany(a => a.AlbumAssignments)
                .HasForeignKey(aua => aua.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indeksy dla LibAlbumUserAssign
            modelBuilder.Entity<LibAlbumUserAssign>()
                .HasIndex(aua => aua.UserId);

            modelBuilder.Entity<LibAlbumUserAssign>()
                .HasIndex(aua => aua.AlbumId);

            // Unikalny indeks - jeden album na użytkownika
            modelBuilder.Entity<LibAlbumUserAssign>()
                .HasIndex(aua => new { aua.UserId, aua.AlbumId })
                .IsUnique();

            // Stronicowanie biblioteki sortowanej po nazwie albumu.
            modelBuilder.Entity<LibAlbumUserAssign>()
                .HasIndex(aua => new { aua.UserId, aua.AlbumName, aua.AlbumId });

            // Konfiguracja relacji many-to-many Track-User
            modelBuilder.Entity<LibTrackUserAssign>()
                .HasOne(tua => tua.User)
                .WithMany(u => u.TrackAssignments)
                .HasForeignKey(tua => tua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LibTrackUserAssign>()
                .HasOne(tua => tua.Track)
                .WithMany(t => t.UserAssignments)
                .HasForeignKey(tua => tua.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indeksy dla LibTrackUserAssign
            modelBuilder.Entity<LibTrackUserAssign>()
                .HasIndex(tua => tua.UserId);

            modelBuilder.Entity<LibTrackUserAssign>()
                .HasIndex(tua => tua.TrackId);

            // Unikalny indeks - jeden utwór na użytkownika
            modelBuilder.Entity<LibTrackUserAssign>()
                .HasIndex(tua => new { tua.UserId, tua.TrackId })
                .IsUnique();

            modelBuilder.Entity<LibSavedFilter>()
                .HasIndex(f => new { f.UserId, f.Scope, f.ContextId, f.Name })
                .IsUnique();

            modelBuilder.ApplyConfigurationsFromAssembly(this.GetType().Assembly);
        }
    }
}
