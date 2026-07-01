# Analisi Tecnica — Love Places
> Documento di specifica tecnica per GitHub Copilot.  
> Stack: ASP.NET Core 10 Web API · Entity Framework Core · SQL Server · .NET MAUI · soluzione multi-progetto.
> **Revisione 2** — place tagging esteso (vibe/musica), connessioni 1:1 come mondi isolati.

---

## 1. Scopo e contesto

**Love Places** è una piattaforma mobile-first per condividere luoghi preferiti ed esperienze con **una persona specifica alla volta**. Ogni connessione tra due utenti è un **mondo isolato**: i posti aggiunti all'interno di una connessione sono visibili solo ai due utenti di quella connessione, e non ad altri — nemmeno ad altri utenti con cui sei connesso.

Non è un social network: nessun feed globale, nessun profilo pubblico, nessuna ricerca di utenti. Le connessioni si creano solo via invito personale.

Un "posto" non è necessariamente un ristorante: può essere un bar, un club, un parco, un museo, un locale con musica live, una spiaggia. Il sistema di tagging riflette questa varietà con categorie di luogo, vibe musicale e atmosfera.

Funzionalità core:
1. Registrazione e login con autenticazione JWT.
2. Creare connessioni 1:1 via codice invito. Ogni connessione è un contesto separato.
3. All'interno di una connessione: aggiungere posti tramite GPS + reverse geocoding.
4. Ogni posto ha: categoria di luogo, vibe musicale, tag atmosfera, tipo di cucina (se food), valutazioni, foto, date di visita.
5. Note per posto: **Privata** (solo l'autore) o **Condivisa** (entrambi gli utenti della connessione).
6. L'utente che apre la connessione vede solo il "mondo" di quella coppia.

---

## 2. Decisione architetturale chiave — Places appartengono a una Connection

> **Questo è il punto più importante per Copilot.**

In Love Places i `Place` **non sono entità globali dell'utente**. Ogni `Place` nasce all'interno di una `UserConnection` specifica e appartiene a quella connessione.

```
UserA ──── ConnectionAB ──── UserB
                │
          Places di AB
          (visibili solo ad A e B)

UserA ──── ConnectionAC ──── UserC
                │
          Places di AC
          (visibili solo ad A e C)
          UserB non vede nulla di questo mondo.
```

Conseguenza: tutta la navigazione API è prefissata con `/api/connections/{connectionId}/...`. Non esistono endpoint globali per i places.

---

## 3. Stack tecnologico

| Layer | Scelta | Note |
|---|---|---|
| Runtime backend | ASP.NET Core 10 Web API | Controller-based REST API |
| ORM | Entity Framework Core 10 + SQL Server | Code-First, migrations |
| Autenticazione | ASP.NET Core Identity + JWT Bearer | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| Frontend | .NET MAUI 10 | MVVM con `CommunityToolkit.Mvvm` |
| Geocoding | Nominatim (OpenStreetMap) | Reverse geocoding via backend; client non chiama Nominatim direttamente |
| Foto | File system locale (MVP) | Cartella `wwwroot/uploads`; sostituibile con Azure Blob Storage |

---

## 4. Struttura della solution (multi-progetto)

```
LovePlaces.sln
│
├── LovePlaces.Api/
│   ├── LovePlaces.Api.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── ConnectionsController.cs       ← gestisce inviti + CRUD connessioni
│   │   ├── PlacesController.cs            ← route: /api/connections/{cid}/places
│   │   ├── NotesController.cs             ← route: /api/connections/{cid}/places/{pid}/notes
│   │   ├── PhotosController.cs
│   │   └── GeocodingController.cs
│   ├── Data/
│   │   └── AppDbContext.cs
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Place.cs
│   │   ├── Note.cs
│   │   ├── Photo.cs
│   │   ├── UserConnection.cs
│   │   ├── Invitation.cs
│   │   └── Enums/
│   │       ├── NoteVisibility.cs
│   │       └── ConnectionStatus.cs
│   ├── Dtos/                              ← DTO server-side (mapping da/verso Models)
│   └── Services/
│       ├── IGeocodingService.cs
│       ├── NominatimGeocodingService.cs
│       ├── ITokenService.cs
│       ├── TokenService.cs
│       ├── IFileStorageService.cs
│       ├── LocalFileStorageService.cs
│       └── IConnectionAuthorizationService.cs  ← helper: verifica che l'utente appartenga alla connessione
│
├── LovePlaces.MAUI/
│   ├── LovePlaces.MAUI.csproj
│   ├── MauiProgram.cs
│   ├── AppShell.xaml
│   ├── Pages/
│   │   ├── LoginPage.xaml
│   │   ├── RegisterPage.xaml
│   │   ├── ConnectionsListPage.xaml      ← homepage: lista delle connessioni dell'utente
│   │   ├── ConnectionDetailPage.xaml     ← "il mondo" di una connessione specifica
│   │   ├── PlaceListPage.xaml            ← posti dentro una connessione
│   │   ├── PlaceDetailPage.xaml
│   │   ├── AddPlacePage.xaml
│   │   └── SettingsPage.xaml
│   ├── ViewModels/
│   │   ├── LoginViewModel.cs
│   │   ├── RegisterViewModel.cs
│   │   ├── ConnectionsListViewModel.cs
│   │   ├── ConnectionDetailViewModel.cs
│   │   ├── PlaceListViewModel.cs
│   │   ├── PlaceDetailViewModel.cs
│   │   └── AddPlaceViewModel.cs
│   └── Services/
│       ├── IApiService.cs
│       ├── ApiService.cs
│       ├── IAuthStateService.cs
│       ├── AuthStateService.cs
│       └── IGeoLocationService.cs
│
└── LovePlaces.Shared/
    ├── LovePlaces.Shared.csproj
    └── Dtos/
        ├── Auth/
        ├── Places/
        ├── Notes/
        ├── Connections/
        └── Geocoding/
```

---

## 5. Sistema di tagging — luoghi non solo food

Un posto può essere di qualsiasi tipo. Il sistema di tagging è organizzato su **tre dimensioni indipendenti**:

| Dimensione | Cardinalità | Colonna DB |
|---|---|---|
| Categoria del luogo (`PlaceType`) | **Singola** (radio) | `PlaceType` (string, max 50) |
| Vibe musicale (`MusicVibes`) | **Multipla** (checkbox) | `MusicVibesJson` (JSON array) |
| Tag atmosfera (`AtmosphereTags`) | **Multipla** (checkbox) | `AtmosphereTagsJson` (JSON array) |
| Tipo di cucina (`CuisineTags`) | **Multipla**, opzionale | `CuisineTagsJson` (JSON array) — rilevante se `PlaceType` è food |

### 5.1 Opzioni predefinite (costanti lato server e client)

```csharp
// Models/PlaceTagOptions.cs
namespace LovePlaces.Api.Models;

public static class PlaceTagOptions
{
    // Categoria del luogo — selezione singola
    public static readonly IReadOnlyList<string> PlaceTypes =
    [
        "Ristorante", "Bar", "Club / Discoteca", "Caffè", "Locale live music",
        "Parco / Natura", "Museo / Galleria", "Spiaggia", "Mercato / Street food",
        "Hotel / Resort", "Negozio", "Altro"
    ];

    // Vibe musicale — selezione multipla
    public static readonly IReadOnlyList<string> MusicVibes =
    [
        "Jazz", "Elettronica / Techno", "Live music", "Indie / Alternative",
        "Hip-hop / R&B", "Classica", "Acustica / Singer-songwriter",
        "Latino / Reggaeton", "Nessuna musica", "Musica di sottofondo"
    ];

    // Atmosfera — selezione multipla
    public static readonly IReadOnlyList<string> AtmosphereTags =
    [
        "Romantico", "Vivace / Energico", "Rilassato / Chill", "Trendy",
        "Tradizionale / Autentico", "All'aperto", "Accogliente / Intimo",
        "Rumoroso / Festaiolo", "Silenzioso / Riflessivo", "Vista panoramica"
    ];

    // Cucina — selezione multipla, mostrata solo per luoghi food
    public static readonly IReadOnlyList<string> CuisineTags =
    [
        "Italiana", "Giapponese / Sushi", "Steakhouse", "Vegetariana / Vegana",
        "Asiatica", "Fusion", "Messicana", "Pesce / Frutti di mare",
        "Pizzeria", "Street food", "Dolci / Pasticceria"
    ];

    // Categorie di luogo considerate "food" (per mostrare/nascondere CuisineTags nel frontend)
    public static readonly IReadOnlySet<string> FoodPlaceTypes =
        new HashSet<string>(["Ristorante", "Caffè", "Mercato / Street food"]);
}
```

---

## 6. Domain model

### 6.1 Enumerazioni

```csharp
// Models/Enums/NoteVisibility.cs
namespace LovePlaces.Api.Models.Enums;

public enum NoteVisibility
{
    Private = 0,   // visibile solo all'autore
    Shared  = 1    // visibile a entrambi gli utenti della connessione
}

// Models/Enums/ConnectionStatus.cs
namespace LovePlaces.Api.Models.Enums;

public enum ConnectionStatus
{
    Pending  = 0,
    Accepted = 1,
    Declined = 2
}
```

### 6.2 User

```csharp
// Models/User.cs
using Microsoft.AspNetCore.Identity;

namespace LovePlaces.Api.Models;

public class User : IdentityUser<Guid>
{
    public string?   DisplayName { get; set; }
    public int?      Age         { get; set; }
    public string?   Gender      { get; set; }
    public string?   AvatarUrl   { get; set; }
    public DateTime  CreatedAt   { get; set; } = DateTime.UtcNow;

    public ICollection<UserConnection> ConnectionsAsUser1 { get; set; } = [];
    public ICollection<UserConnection> ConnectionsAsUser2 { get; set; } = [];
    public ICollection<Invitation>     SentInvitations    { get; set; } = [];
    public ICollection<Place>          AuthoredPlaces     { get; set; } = [];
    public ICollection<Note>           AuthoredNotes      { get; set; } = [];
}
```

### 6.3 UserConnection

```csharp
// Models/UserConnection.cs
using LovePlaces.Api.Models.Enums;

namespace LovePlaces.Api.Models;

// Una connessione 1:1 è un contesto isolato. I Place di questa connessione
// sono visibili SOLO ai due utenti qui referenziati.
public class UserConnection
{
    public int  Id      { get; set; }
    public Guid User1Id { get; set; }   // per convenzione User1Id < User2Id (Guid.CompareTo)
    public Guid User2Id { get; set; }

    public ConnectionStatus Status    { get; set; } = ConnectionStatus.Accepted;
    public DateTime         CreatedAt { get; set; } = DateTime.UtcNow;

    // Ogni connessione ha il proprio universo di posti
    public ICollection<Place>      Places      { get; set; } = [];

    public User User1 { get; set; } = null!;
    public User User2 { get; set; } = null!;
}
```

### 6.4 Place

```csharp
// Models/Place.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace LovePlaces.Api.Models;

public class Place
{
    public int  Id           { get; set; }
    public int  ConnectionId { get; set; }   // FK: il posto appartiene a questa connessione
    public Guid AuthorId     { get; set; }   // quale dei due utenti ha aggiunto il posto

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Address { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public double? Latitude  { get; set; }
    public double? Longitude { get; set; }

    [Required]
    public DateOnly DateVisited { get; set; }

    // ── Categoria del luogo (selezione singola) ─────────────────────────
    [MaxLength(50)]
    public string PlaceType { get; set; } = "Altro";

    // ── Tag per dimensione (JSON arrays) ────────────────────────────────
    public string MusicVibesJson    { get; set; } = "[]";
    public string AtmosphereTagsJson{ get; set; } = "[]";
    public string CuisineTagsJson   { get; set; } = "[]";  // vuoto se non food

    // ── Valutazioni ─────────────────────────────────────────────────────
    [Range(1, 5)] public int Atmosphere    { get; set; }
    [Range(1, 5)] public int ValueForMoney { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Proprietà calcolate [NotMapped]
    [NotMapped]
    public List<string> MusicVibes
    {
        get => JsonSerializer.Deserialize<List<string>>(MusicVibesJson) ?? [];
        set => MusicVibesJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<string> AtmosphereTags
    {
        get => JsonSerializer.Deserialize<List<string>>(AtmosphereTagsJson) ?? [];
        set => AtmosphereTagsJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public List<string> CuisineTags
    {
        get => JsonSerializer.Deserialize<List<string>>(CuisineTagsJson) ?? [];
        set => CuisineTagsJson = JsonSerializer.Serialize(value);
    }

    // Navigation properties
    public UserConnection     Connection { get; set; } = null!;
    public User               Author     { get; set; } = null!;
    public ICollection<Note>  Notes      { get; set; } = [];
    public ICollection<Photo> Photos     { get; set; } = [];
}
```

### 6.5 Note

```csharp
// Models/Note.cs
using System.ComponentModel.DataAnnotations;
using LovePlaces.Api.Models.Enums;

namespace LovePlaces.Api.Models;

public class Note
{
    public int  Id      { get; set; }
    public int  PlaceId { get; set; }
    public Guid AuthorId{ get; set; }

    [Required, MaxLength(4000)]
    public string Text { get; set; } = string.Empty;

    // Private: solo l'autore; Shared: entrambi gli utenti della connessione del posto
    public NoteVisibility Visibility { get; set; } = NoteVisibility.Private;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Place Place  { get; set; } = null!;
    public User  Author { get; set; } = null!;
}
```

### 6.6 Photo

```csharp
// Models/Photo.cs
using System.ComponentModel.DataAnnotations;

namespace LovePlaces.Api.Models;

public class Photo
{
    public int  Id       { get; set; }
    public int  PlaceId  { get; set; }
    public Guid AuthorId { get; set; }

    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Caption { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Place Place  { get; set; } = null!;
    public User  Author { get; set; } = null!;
}
```

### 6.7 Invitation

```csharp
// Models/Invitation.cs
using System.ComponentModel.DataAnnotations;

namespace LovePlaces.Api.Models;

public class Invitation
{
    public int  Id       { get; set; }
    public Guid SenderId { get; set; }

    [Required, MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    public bool     IsUsed   { get; set; } = false;
    public Guid?    UsedById { get; set; }
    public DateTime CreatedAt{ get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt{ get; set; }   // default: CreatedAt + 7 giorni

    public User  Sender { get; set; } = null!;
    public User? UsedBy { get; set; }
}
```

---

## 7. DbContext

```csharp
// Data/AppDbContext.cs
using LovePlaces.Api.Models;
using LovePlaces.Api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LovePlaces.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserConnection> UserConnections { get; set; }
    public DbSet<Place>          Places          { get; set; }
    public DbSet<Note>           Notes           { get; set; }
    public DbSet<Photo>          Photos          { get; set; }
    public DbSet<Invitation>     Invitations     { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── User ──────────────────────────────────────────────────────────
        builder.Entity<User>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(100);
            e.Property(u => u.Gender).HasMaxLength(50);
            e.Property(u => u.AvatarUrl).HasMaxLength(500);
        });

        // ── UserConnection ────────────────────────────────────────────────
        builder.Entity<UserConnection>(e =>
        {
            e.ToTable("UserConnections");
            e.HasKey(uc => uc.Id);
            e.Property(uc => uc.Status).HasConversion<int>();

            // Coppia unica: una sola connessione per due utenti
            // Convenzione enforced nel service layer: User1Id < User2Id (Guid.CompareTo)
            e.HasIndex(uc => new { uc.User1Id, uc.User2Id }).IsUnique();

            e.HasOne(uc => uc.User1)
             .WithMany(u => u.ConnectionsAsUser1)
             .HasForeignKey(uc => uc.User1Id)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(uc => uc.User2)
             .WithMany(u => u.ConnectionsAsUser2)
             .HasForeignKey(uc => uc.User2Id)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Place ─────────────────────────────────────────────────────────
        builder.Entity<Place>(e =>
        {
            e.ToTable("Places");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Address).HasMaxLength(300);
            e.Property(p => p.Description).HasMaxLength(2000);
            e.Property(p => p.PlaceType).HasMaxLength(50).HasDefaultValue("Altro");
            e.Property(p => p.MusicVibesJson).HasDefaultValue("[]");
            e.Property(p => p.AtmosphereTagsJson).HasDefaultValue("[]");
            e.Property(p => p.CuisineTagsJson).HasDefaultValue("[]");

            // Un posto appartiene a una connessione — cascade delete:
            // eliminare la connessione elimina tutti i suoi posti
            e.HasOne(p => p.Connection)
             .WithMany(c => c.Places)
             .HasForeignKey(p => p.ConnectionId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(p => p.Author)
             .WithMany(u => u.AuthoredPlaces)
             .HasForeignKey(p => p.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(p => p.ConnectionId);
            e.HasIndex(p => p.DateVisited);
        });

        // ── Note ──────────────────────────────────────────────────────────
        builder.Entity<Note>(e =>
        {
            e.ToTable("Notes");
            e.HasKey(n => n.Id);
            e.Property(n => n.Text).IsRequired().HasMaxLength(4000);
            e.Property(n => n.Visibility).HasConversion<int>();

            e.HasOne(n => n.Place)
             .WithMany(p => p.Notes)
             .HasForeignKey(n => n.PlaceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(n => n.Author)
             .WithMany(u => u.AuthoredNotes)
             .HasForeignKey(n => n.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Photo ─────────────────────────────────────────────────────────
        builder.Entity<Photo>(e =>
        {
            e.ToTable("Photos");
            e.HasKey(ph => ph.Id);
            e.Property(ph => ph.FilePath).IsRequired().HasMaxLength(500);
            e.Property(ph => ph.Caption).HasMaxLength(300);

            e.HasOne(ph => ph.Place)
             .WithMany(p => p.Photos)
             .HasForeignKey(ph => ph.PlaceId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ph => ph.Author)
             .WithMany()
             .HasForeignKey(ph => ph.AuthorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Invitation ────────────────────────────────────────────────────
        builder.Entity<Invitation>(e =>
        {
            e.ToTable("Invitations");
            e.HasKey(i => i.Id);
            e.Property(i => i.Code).IsRequired().HasMaxLength(64);
            e.HasIndex(i => i.Code).IsUnique();

            e.HasOne(i => i.Sender)
             .WithMany(u => u.SentInvitations)
             .HasForeignKey(i => i.SenderId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(i => i.UsedBy)
             .WithMany()
             .HasForeignKey(i => i.UsedById)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
```

---

## 8. ConnectionAuthorizationService

> Questo helper è usato da tutti i controller che operano su risorse di una connessione.
> Garantisce che l'utente autenticato sia uno dei due membri della connessione richiesta.

```csharp
// Services/IConnectionAuthorizationService.cs
namespace LovePlaces.Api.Services;

public interface IConnectionAuthorizationService
{
    /// <summary>
    /// Ritorna la connessione se esiste e se currentUserId è User1 o User2.
    /// Ritorna null se la connessione non esiste o l'utente non ne fa parte.
    /// </summary>
    Task<UserConnection?> GetAuthorizedConnectionAsync(int connectionId, Guid currentUserId, CancellationToken ct = default);
}

// Services/ConnectionAuthorizationService.cs
using LovePlaces.Api.Data;
using LovePlaces.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LovePlaces.Api.Services;

public class ConnectionAuthorizationService(AppDbContext db) : IConnectionAuthorizationService
{
    public async Task<UserConnection?> GetAuthorizedConnectionAsync(int connectionId, Guid currentUserId, CancellationToken ct = default)
    {
        var connection = await db.UserConnections
            .FirstOrDefaultAsync(c => c.Id == connectionId &&
                                      (c.User1Id == currentUserId || c.User2Id == currentUserId), ct);
        return connection; // null se non trovata o non autorizzata
    }
}
```

---

## 9. Autenticazione — JWT

```csharp
// Services/ITokenService.cs + TokenService.cs
// Identico alla versione precedente — genera JWT con claim: NameIdentifier (Guid), Email, Name
// Il controller legge sempre l'userId dal claim, mai dal body della request.
```

---

## 10. Surface API

> ⚠️ **Tutti gli endpoint richiedono `[Authorize]` (JWT Bearer), eccetto `/api/auth/*`.**
> Il `userId` corrente si ricava sempre da `User.FindFirstValue(ClaimTypes.NameIdentifier)`.
> Prima di operare su qualsiasi risorsa di una connessione, chiamare `IConnectionAuthorizationService.GetAuthorizedConnectionAsync()` e ritornare `403 Forbidden` se null.

### Auth

| Metodo | Route | Body | Risposta |
|---|---|---|---|
| `POST` | `/api/auth/register` | `RegisterRequestDto` | `LoginResponseDto` |
| `POST` | `/api/auth/login` | `LoginRequestDto` | `LoginResponseDto` |

### Connections

| Metodo | Route | Descrizione |
|---|---|---|
| `GET` | `/api/connections` | Le mie connessioni (lista con info del partner e conteggio posti) |
| `POST` | `/api/connections/invite` | Genera codice invito (7 giorni, monouso) |
| `POST` | `/api/connections/accept/{code}` | Accetta un invito → crea la `UserConnection` |
| `DELETE` | `/api/connections/{id}` | Rimuove una connessione (cascade su tutti i suoi Places) |

### Places — scoped alla connessione

| Metodo | Route | Descrizione |
|---|---|---|
| `GET` | `/api/connections/{cid}/places` | Tutti i posti della connessione, ordinati per `DateVisited DESC` |
| `GET` | `/api/connections/{cid}/places/{id}` | Singolo posto |
| `POST` | `/api/connections/{cid}/places` | Aggiunge un posto alla connessione |
| `PUT` | `/api/connections/{cid}/places/{id}` | Aggiorna (solo l'autore del posto) |
| `DELETE` | `/api/connections/{cid}/places/{id}` | Elimina (solo l'autore) |

### Notes — scoped al posto (e quindi alla connessione)

| Metodo | Route | Descrizione |
|---|---|---|
| `GET` | `/api/connections/{cid}/places/{pid}/notes` | Note visibili all'utente corrente (proprie + shared altrui) |
| `POST` | `/api/connections/{cid}/places/{pid}/notes` | Aggiunge una nota |
| `PUT` | `/api/connections/{cid}/notes/{id}` | Modifica nota (solo l'autore) |
| `DELETE` | `/api/connections/{cid}/notes/{id}` | Elimina nota (solo l'autore) |

### Photos

| Metodo | Route | Descrizione |
|---|---|---|
| `GET` | `/api/connections/{cid}/places/{pid}/photos` | Foto del posto |
| `POST` | `/api/connections/{cid}/places/{pid}/photos` | Upload (`multipart/form-data`) |
| `DELETE` | `/api/connections/{cid}/photos/{id}` | Elimina (solo chi l'ha caricata) |

### Geocoding

| Metodo | Route | Body | Risposta |
|---|---|---|---|
| `POST` | `/api/geocoding/reverse` | `{ "lat": 44.49, "lng": 11.34 }` | `{ "placeName": "...", "address": "..." }` |

### Tag options (per il client)

| Metodo | Route | Descrizione |
|---|---|---|
| `GET` | `/api/tags` | Ritorna `PlaceTagOptions` completo (placeTypes, musicVibes, atmosphereTags, cuisineTags, foodPlaceTypes) |

---

## 11. DTO (LovePlaces.Shared)

```csharp
// Dtos/Auth/RegisterRequestDto.cs
public class RegisterRequestDto
{
    [Required, EmailAddress]  public string Email       { get; set; } = string.Empty;
    [Required, MinLength(3)]  public string Username    { get; set; } = string.Empty;
    [Required, MinLength(8)]  public string Password    { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public int?    Age         { get; set; }
    public string? Gender      { get; set; }
}

// Dtos/Auth/LoginRequestDto.cs
public class LoginRequestDto
{
    public string Email    { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// Dtos/Auth/LoginResponseDto.cs
public class LoginResponseDto
{
    public string   Token       { get; set; } = string.Empty;
    public DateTime ExpiresAt   { get; set; }
    public string   Username    { get; set; } = string.Empty;
    public string?  DisplayName { get; set; }
}

// Dtos/Connections/ConnectionResponseDto.cs
public class ConnectionResponseDto
{
    public int     Id                { get; set; }
    public string  PartnerUserId     { get; set; } = string.Empty;  // Guid come stringa
    public string  PartnerName       { get; set; } = string.Empty;
    public string? PartnerAvatarUrl  { get; set; }
    public int     PlaceCount        { get; set; }
    public DateTime ConnectedSince   { get; set; }
}

// Dtos/Connections/InvitationResponseDto.cs
public class InvitationResponseDto
{
    public string   Code      { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    // Il link è: "loveplaces://invite/{Code}"
}

// Dtos/Places/PlaceCreateDto.cs
public class PlaceCreateDto
{
    [Required, MaxLength(200)] public string Name         { get; set; } = string.Empty;
    [MaxLength(300)]           public string? Address     { get; set; }
    [MaxLength(2000)]          public string? Description { get; set; }
    public double?   Latitude     { get; set; }
    public double?   Longitude    { get; set; }
    [Required]       public DateOnly DateVisited  { get; set; }

    [MaxLength(50)]  public string PlaceType { get; set; } = "Altro";

    public List<string> MusicVibes     { get; set; } = [];
    public List<string> AtmosphereTags { get; set; } = [];
    public List<string> CuisineTags    { get; set; } = [];

    [Range(1, 5)] public int Atmosphere    { get; set; }
    [Range(1, 5)] public int ValueForMoney { get; set; }
}

// Dtos/Places/PlaceResponseDto.cs
public class PlaceResponseDto
{
    public int      Id            { get; set; }
    public int      ConnectionId  { get; set; }
    public string   Name          { get; set; } = string.Empty;
    public string?  Address       { get; set; }
    public string?  Description   { get; set; }
    public double?  Latitude      { get; set; }
    public double?  Longitude     { get; set; }
    public DateOnly DateVisited   { get; set; }
    public string   PlaceType     { get; set; } = string.Empty;
    public List<string> MusicVibes     { get; set; } = [];
    public List<string> AtmosphereTags { get; set; } = [];
    public List<string> CuisineTags    { get; set; } = [];
    public int      Atmosphere    { get; set; }
    public int      ValueForMoney { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public string   AuthorName    { get; set; } = string.Empty;
    public bool     IsAuthor      { get; set; }  // true se il richiedente è chi ha aggiunto il posto
    public DateTime CreatedAt     { get; set; }
}

// Dtos/Notes/NoteCreateDto.cs
public class NoteCreateDto
{
    [Required, MaxLength(4000)] public string Text       { get; set; } = string.Empty;
    public int Visibility { get; set; } = 0; // 0 = Private, 1 = Shared
}

// Dtos/Notes/NoteResponseDto.cs
public class NoteResponseDto
{
    public int      Id         { get; set; }
    public string   Text       { get; set; } = string.Empty;
    public int      Visibility { get; set; }
    public string   AuthorName { get; set; } = string.Empty;
    public bool     IsOwn      { get; set; }
    public DateTime CreatedAt  { get; set; }
    public DateTime UpdatedAt  { get; set; }
}
```

---

## 12. Configurazione backend (Program.cs)

```csharp
using System.Text;
using LovePlaces.Api.Data;
using LovePlaces.Api.Models;
using LovePlaces.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit   = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var jwtConfig = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!)),
        ValidateIssuer           = true, ValidIssuer  = jwtConfig["Issuer"],
        ValidateAudience         = true, ValidAudience= jwtConfig["Audience"],
        ValidateLifetime         = true, ClockSkew    = TimeSpan.Zero
    };
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IConnectionAuthorizationService, ConnectionAuthorizationService>();
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org");
    client.DefaultRequestHeaders.Add("User-Agent", "LovePlaces/1.0");
    client.Timeout     = TimeSpan.FromSeconds(10);
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 13. appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=LovePlacesDb;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key":        "SOSTITUIRE_CON_CHIAVE_SEGRETA_MIN_32_CHAR",
    "Issuer":     "LovePlaces.Api",
    "Audience":   "LovePlaces.MAUI",
    "ExpiryDays": "30"
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*"
}
```

---

## 14. MAUI — architettura frontend

### 14.1 Shell navigation

```
AppShell
├── (no auth)  LoginPage, RegisterPage
└── (autenticato)
    ├── ConnectionsListPage          ← homepage: "le mie connessioni"
    │   └── → ConnectionDetailPage  ← "il mondo" di una connessione (mappa + lista posti)
    │       └── → PlaceDetailPage   ← dettaglio singolo posto
    │           └── → AddPlacePage  ← aggiunta posto (con GPS)
    └── SettingsPage

Route parametriche:
    "connections/detail?connectionId={id}"
    "connections/{cid}/places/detail?placeId={id}"
    "connections/{cid}/places/add"
```

La `ConnectionsListPage` è la vera homepage: mostra tutte le connessioni dell'utente, ognuna come una "stanza" separata con nome del partner e numero di posti condivisi.

### 14.2 AuthStateService

```csharp
// Usa SecureStorage per persistere il JWT tra le sessioni (non Preferences)
public class AuthStateService : IAuthStateService
{
    private const string TokenKey = "jwt_token";
    public string? Token     { get; private set; }
    public bool IsLoggedIn   => !string.IsNullOrEmpty(Token);
    public Guid   CurrentUserId { get; private set; }  // parsato dal JWT al momento del salvataggio

    public async Task SaveTokenAsync(string token)
    {
        Token = token;
        // Parsare il JWT per estrarre il sub claim (userId)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt     = handler.ReadJwtToken(token);
        CurrentUserId = Guid.Parse(jwt.Subject);
        await SecureStorage.SetAsync(TokenKey, token);
    }

    public async Task LoadTokenAsync()
    {
        Token = await SecureStorage.GetAsync(TokenKey);
        if (Token is not null)
        {
            var handler   = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwt       = handler.ReadJwtToken(Token);
            CurrentUserId = Guid.Parse(jwt.Subject);
        }
    }

    public async Task ClearTokenAsync() { Token = null; SecureStorage.Remove(TokenKey); await Task.CompletedTask; }
}
```

### 14.3 IApiService — superficie completa

```csharp
public interface IApiService
{
    // Auth
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto dto);
    Task<LoginResponseDto?> RegisterAsync(RegisterRequestDto dto);

    // Connections
    Task<List<ConnectionResponseDto>> GetConnectionsAsync();
    Task<InvitationResponseDto?>      GenerateInviteAsync();
    Task<ConnectionResponseDto?>      AcceptInviteAsync(string code);
    Task                              DeleteConnectionAsync(int connectionId);

    // Places (tutti scoped a connectionId)
    Task<List<PlaceResponseDto>> GetPlacesAsync(int connectionId);
    Task<PlaceResponseDto?>      GetPlaceAsync(int connectionId, int placeId);
    Task<PlaceResponseDto?>      CreatePlaceAsync(int connectionId, PlaceCreateDto dto);
    Task<PlaceResponseDto?>      UpdatePlaceAsync(int connectionId, int placeId, PlaceCreateDto dto);
    Task                         DeletePlaceAsync(int connectionId, int placeId);

    // Notes
    Task<List<NoteResponseDto>> GetNotesAsync(int connectionId, int placeId);
    Task<NoteResponseDto?>      AddNoteAsync(int connectionId, int placeId, NoteCreateDto dto);
    Task                        DeleteNoteAsync(int connectionId, int noteId);

    // Geocoding
    Task<(string PlaceName, string? Address)?> ReverseGeocodeAsync(double lat, double lng);

    // Tags
    Task<PlaceTagOptionsDto?> GetTagOptionsAsync();
}
```

### 14.4 AddPlaceViewModel

```csharp
public partial class AddPlaceViewModel(
    IApiService api,
    IGeoLocationService geo,
    IAuthStateService auth,
    [FromQuery] int connectionId) : ObservableObject
{
    [ObservableProperty] private string  _name           = string.Empty;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private double? _latitude;
    [ObservableProperty] private double? _longitude;
    [ObservableProperty] private string  _placeType      = "Altro";
    [ObservableProperty] private int     _atmosphere     = 3;
    [ObservableProperty] private int     _valueForMoney  = 3;
    [ObservableProperty] private bool    _isBusy;
    [ObservableProperty] private string? _statusMessage;

    // Selezioni multiple — il ViewModel espone le opzioni e traccia quelle selezionate
    public ObservableCollection<SelectableTag> MusicVibeOptions     { get; } = [];
    public ObservableCollection<SelectableTag> AtmosphereTagOptions { get; } = [];
    public ObservableCollection<SelectableTag> CuisineTagOptions    { get; } = [];

    // true se PlaceType è food (mostra/nasconde CuisineTagOptions nella view)
    public bool IsFoodPlace => PlaceTagOptions.FoodPlaceTypes.Contains(PlaceType);

    [RelayCommand]
    private async Task DetectLocationAsync()
    {
        IsBusy = true; StatusMessage = "Rilevamento posizione...";
        var coords = await geo.GetCurrentLocationAsync();
        if (coords is null) { StatusMessage = "Posizione non disponibile."; IsBusy = false; return; }
        Latitude = coords.Value.Lat; Longitude = coords.Value.Lng;
        StatusMessage = "Geocoding...";
        var result = await api.ReverseGeocodeAsync(coords.Value.Lat, coords.Value.Lng);
        if (result.HasValue) { Name = result.Value.PlaceName; Address = result.Value.Address; }
        StatusMessage = null; IsBusy = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Name)) return;
        IsBusy = true;
        var dto = new PlaceCreateDto
        {
            Name          = Name, Address = Address, Description = Description,
            Latitude      = Latitude, Longitude = Longitude,
            DateVisited   = DateOnly.FromDateTime(DateTime.Today),
            PlaceType     = PlaceType,
            MusicVibes    = [.. MusicVibeOptions.Where(t => t.IsSelected).Select(t => t.Value)],
            AtmosphereTags= [.. AtmosphereTagOptions.Where(t => t.IsSelected).Select(t => t.Value)],
            CuisineTags   = IsFoodPlace ? [.. CuisineTagOptions.Where(t => t.IsSelected).Select(t => t.Value)] : [],
            Atmosphere    = Atmosphere, ValueForMoney = ValueForMoney
        };
        var result = await api.CreatePlaceAsync(connectionId, dto);
        IsBusy = false;
        if (result is not null) await Shell.Current.GoToAsync("..");
    }
}

// Helper per i tag selezionabili
public partial class SelectableTag(string value) : ObservableObject
{
    public string Value { get; } = value;
    [ObservableProperty] private bool _isSelected;
}
```

---

## 15. Logica di autorizzazione — regole chiave

| Regola | Implementazione |
|---|---|
| Ogni endpoint su connessione/posto/nota chiama `IConnectionAuthorizationService` | `var conn = await connAuth.GetAuthorizedConnectionAsync(cid, currentUserId);` `if (conn is null) return Forbid();` |
| Solo l'autore di un posto può modificarlo o eliminarlo | `if (place.AuthorId != currentUserId) return Forbid();` |
| Le note `Private` non vengono mai incluse nella risposta se `note.AuthorId != currentUserId` | Filtrare nel controller: `.Where(n => n.Visibility == Shared || n.AuthorId == currentUserId)` |
| Solo l'autore di una nota può modificarla o eliminarla | `if (note.AuthorId != currentUserId) return Forbid();` |
| Un utente non può vedere i posti di connessioni a cui non appartiene | `GetAuthorizedConnectionAsync` ritorna null → 403 |
| Convenzione User1Id < User2Id | `var (u1, u2) = userId1 < userId2 ? (userId1, userId2) : (userId2, userId1);` — enforced nel service layer prima di creare o cercare la `UserConnection` |
| Invito: un codice può essere accettato solo da un utente diverso dal mittente | `if (invitation.SenderId == currentUserId) return BadRequest("Non puoi accettare il tuo stesso invito.");` |
| Invito scaduto o già usato | `if (invitation.IsUsed || invitation.ExpiresAt < DateTime.UtcNow) return BadRequest(...)` |

---

## 16. EF Core — comandi migration

```bash
dotnet ef migrations add InitialCreate --project LovePlaces.Api --startup-project LovePlaces.Api
dotnet ef database update              --project LovePlaces.Api --startup-project LovePlaces.Api
dotnet ef migrations add <NomeMigration> --project LovePlaces.Api --startup-project LovePlaces.Api
```

---

## 17. NuGet packages

### LovePlaces.Api.csproj

```xml
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer"           Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design"              Version="10.*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools"               Version="10.*">
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer"    Version="10.*" />
<PackageReference Include="Microsoft.IdentityModel.Tokens"                   Version="8.*"  />
<PackageReference Include="Swashbuckle.AspNetCore"                           Version="7.*"  />
```

### LovePlaces.MAUI.csproj

```xml
<PackageReference Include="CommunityToolkit.Mvvm"          Version="8.*"  />
<PackageReference Include="CommunityToolkit.Maui"          Version="9.*"  />
<PackageReference Include="Microsoft.Maui.Controls.Maps"   Version="10.*" />
<PackageReference Include="Microsoft.Extensions.Http"      Version="10.*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.*" />
```

### LovePlaces.Shared.csproj

```xml
<!-- Solo POCO + DataAnnotations, nessuna dipendenza da EF o MAUI -->
```

---

## 18. Vincoli e note implementative

| Vincolo | Dettaglio |
|---|---|
| **Places appartengono a una Connection** | Non esiste un endpoint globale per i places. Tutto è sotto `/api/connections/{cid}/...` |
| **Connessioni isolate** | User B non vede mai i posti delle connessioni di User A che non lo includono. L'isolamento è garantito a livello di FK: `Place.ConnectionId` |
| **PlaceType opzionale** | Se l'utente non seleziona un tipo, il default è `"Altro"`. `CuisineTags` viene ignorato (array vuoto) per tipi non-food |
| **MusicVibes sempre disponibile** | Indipendentemente dal PlaceType, il campo musica è sempre compilabile |
| **Nessun profilo pubblico** | L'API espone solo `DisplayName` e `AvatarUrl` del partner, mai email o altri dati sensibili |
| **JWT nel SecureStorage MAUI** | Non usare `Preferences` per il token |
| **Auto-migrate** | `db.Database.Migrate()` in Program.cs è accettabile per sviluppo/MVP |
| **Foto** | Max 5MB, formati JPEG/PNG/WEBP. Validare `ContentType` prima di `IFileStorageService` |
| **Nominatim** | Max 1 req/sec. Il geocoding passa sempre per l'API backend |
| **Deep link invito** | `loveplaces://invite/{code}` — registrare lo URI scheme in AndroidManifest.xml e Info.plist |
| **Cascade delete Connection** | Eliminare una `UserConnection` elimina a cascata tutti i suoi `Places`, e questi a cascata `Notes` e `Photos` (file fisici vanno eliminati manualmente prima via `IFileStorageService`) |