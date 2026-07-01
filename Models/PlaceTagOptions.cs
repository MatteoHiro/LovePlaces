namespace LovePlaceApp.Models;

public static class PlaceTagOptions
{
    public static readonly IReadOnlyList<string> PlaceTypes =
    [
        "Ristorante", "Bar", "Club / Discoteca", "Caffe", "Locale live music",
        "Parco / Natura", "Museo / Galleria", "Spiaggia", "Mercato / Street food",
        "Hotel / Resort", "Negozio", "Altro"
    ];

    public static readonly IReadOnlyList<string> MusicVibes =
    [
        "Jazz", "Elettronica / Techno", "Live music", "Indie / Alternative",
        "Hip-hop / R&B", "Classica", "Acustica / Singer-songwriter",
        "Latino / Reggaeton", "Nessuna musica", "Musica di sottofondo"
    ];

    public static readonly IReadOnlyList<string> AtmosphereTags =
    [
        "Romantico", "Vivace / Energico", "Rilassato / Chill", "Trendy",
        "Tradizionale / Autentico", "All'aperto", "Accogliente / Intimo",
        "Rumoroso / Festaiolo", "Silenzioso / Riflessivo", "Vista panoramica"
    ];

    public static readonly IReadOnlyList<string> CuisineTags =
    [
        "Italiana", "Giapponese / Sushi", "Steakhouse", "Vegetariana / Vegana",
        "Asiatica", "Fusion", "Messicana", "Pesce / Frutti di mare",
        "Pizzeria", "Street food", "Dolci / Pasticceria"
    ];

    public static readonly IReadOnlySet<string> FoodPlaceTypes =
        new HashSet<string>(["Ristorante", "Caffe", "Mercato / Street food"]);
}
