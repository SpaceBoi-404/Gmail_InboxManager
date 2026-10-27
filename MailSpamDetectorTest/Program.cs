using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Wczytywanie kluczy z pliku credentials.json
var secretJson = File.ReadAllText("credentials.json");
var secrets = JsonSerializer.Deserialize<JsonElement>(secretJson).GetProperty("web");
var clientId = secrets.GetProperty("client_id").GetString();
var clientSecret = secrets.GetProperty("client_secret").GetString();

// Ten adres musi być IDENTYCZNY z tym w Google Cloud
// Zmień port na swój z localhost, a gdy wgramy na Azure zmienisz na adres z Azure
string redirectUri = "https://google-integration-test-a8d2frg6dpece8ek.polandcentral-01.azurewebsites.net/callback";

var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
{
    ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
    Scopes = new[] { GmailService.Scope.GmailModify },
    DataStore = new FileDataStore(Path.GetTempPath(), true)
});

// 1. ENDPOINT LOGOWANIA - Zwraca link do Google
app.MapGet("/login", () => {
    var authRequest = flow.CreateAuthorizationCodeRequest(redirectUri);
    return Results.Redirect(authRequest.Build().ToString());
});

// 2. ENDPOINT CALLBACK - Odbiera token od Google po logowaniu
app.MapGet("/callback", async (string code) => {
    // Zapisuje token w pliku token.json dla użytkownika "user"
    await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, CancellationToken.None);
    return Results.Ok("Zalogowano pomyślnie! Zamknij tę kartę i wejdź na /api/test-gmail");
});

// 3. ENDPOINT AKCJI - Działa w tle na podstawie zapisanego tokenu
app.MapGet("/api/test-gmail", async () => {
    var token = await flow.DataStore.GetAsync<TokenResponse>("user");
    if (token == null) return Results.Redirect("/login");

    var credential = new UserCredential(flow, "user", token);
    var service = new GmailService(new BaseClientService.Initializer()
    {
        HttpClientInitializer = credential,
        ApplicationName = "SpamDetector API"
    });

    // 1. Pobranie listy wiadomości (tylko ID)
    var request = service.Users.Messages.List("me");
    request.MaxResults = 1;
    var response = await request.ExecuteAsync();

    if (response.Messages == null || response.Messages.Count == 0)
        return Results.Ok("Brak wiadomości.");

    var msgId = response.Messages[0].Id;

    // 2. NOWOŚĆ: Pobranie PEŁNEJ wiadomości po ID
    var getRequest = service.Users.Messages.Get("me", msgId);
    getRequest.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
    var fullMsg = await getRequest.ExecuteAsync();

    // 3. Wyciągnięcie tematu z nagłówków
    var subject = fullMsg.Payload.Headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(Brak tematu)";

    // 4. Wyciągnięcie i odkodowanie treści (Base64Url)
    string encodedBody = string.Empty;

    // Maile z załącznikami i HTML mają strukturę wieloczęściową (Parts)
    if (fullMsg.Payload.Parts != null)
    {
        var textPart = fullMsg.Payload.Parts.FirstOrDefault(p => p.MimeType == "text/plain");
        encodedBody = textPart?.Body?.Data ?? "";
    }
    else
    {
        // Proste maile mają treść bezpośrednio w Payload
        encodedBody = fullMsg.Payload.Body?.Data ?? "";
    }

    // Funkcja dekodująca specjalny format Gmaila do zwykłego polskiego tekstu
    string decodedText = "";
    if (!string.IsNullOrEmpty(encodedBody))
    {
        string base64 = encodedBody.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        decodedText = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    // Zwracamy piękny, odkodowany wynik jako JSON
    return Results.Ok(new
    {
        status = "Sukces!",
        id = msgId,
        temat = subject,
        tresc = decodedText
    });
});

app.Run();