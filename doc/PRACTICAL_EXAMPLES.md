# 💡 Exemples Pratiques - Système d'Authentification NetAdminPro

**Version:** 1.0  
**Date:** 4 Février 2026  
**Public:** Développeurs

---

## 🎯 Table des Matières

1. [Exemples Serveur](#exemples-serveur)
2. [Exemples Client](#exemples-client)
3. [Gestion des Erreurs](#gestion-des-erreurs)
4. [Tests Unitaires](#tests-unitaires)
5. [Configuration](#configuration)

---

## 📌 Exemples Serveur

### Exemple 1: Initialiser le Service d'Authentification

```csharp
// Dans Program.cs du serveur
using NetAdmin.Server.Data;
using NetAdmin.Server.Services;

// 1. Créer le contexte de base de données
var context = new AppDbContext();

// 2. Créer le service d'authentification
var jwtSecret = "votre-secret-super-secret-de-32-caracteres-minimum!!!";
var authService = new AuthenticationService(
    context: context,
    jwtSecret: jwtSecret,
    tokenExpirationMinutes: 60,
    refreshTokenExpirationDays: 7
);

// 3. Initialiser la base de données
var initializer = new DatabaseInitializer(context, authService);
initializer.Initialize();

// 4. Créer le session manager
var sessionManager = new SessionManager();

// 5. Abonner aux événements
sessionManager.OnSessionCreated += (session) =>
{
    Console.WriteLine($"[AUTH] Nouvelle session: {session.Username}");
};

sessionManager.OnSessionClosed += (clientId) =>
{
    Console.WriteLine($"[AUTH] Session fermée: {clientId}");
};

// Serveur prêt!
var server = new TcpServer(5000);
server.Start();

Console.WriteLine("Serveur d'authentification démarré!");
Console.ReadLine();
```

---

### Exemple 2: Traiter une Requête de Connexion dans TcpServer

```csharp
private async Task HandleClientAsync(TcpClient client, CancellationToken token)
{
    string clientId = client.Client.RemoteEndPoint.ToString();
    NetworkStream stream = client.GetStream();

    try
    {
        while (!token.IsCancellationRequested && client.Connected)
        {
            // ... Lire le paquet
            var packet = JsonSerializer.Deserialize<NetworkPacket>(json);

            // ===== TRAITER LA CONNEXION =====
            if (packet.Type == PacketType.Login)
            {
                var loginRequest = packet.DeserializePayload<LoginRequest>();
                
                // Valider les credentials
                var response = _authService.Authenticate(
                    loginRequest, 
                    clientId
                );

                // Si succès, créer une session
                if (response.Success)
                {
                    _sessionManager.CreateSession(
                        clientId, 
                        response, 
                        clientId
                    );
                    
                    Console.WriteLine(
                        $"✓ {response.Username} s'est connecté avec succès"
                    );
                }
                else
                {
                    Console.WriteLine($"✗ Tentative de connexion échouée");
                }

                // Envoyer la réponse au client
                var responsePacket = NetworkPacket.Create(
                    PacketType.LoginResponse,
                    "SERVER",
                    response
                );
                
                await SendToClient(clientId, responsePacket);
            }

            // ===== TRAITER UNE REQUÊTE PROTÉGÉE =====
            else if (RequiresAuthentication(packet.Type))
            {
                // Vérifier le token
                if (string.IsNullOrEmpty(packet.AuthToken))
                {
                    SendError(clientId, "Token d'authentification requis");
                    continue;
                }

                var validation = _authService.ValidateToken(packet.AuthToken);
                
                if (!validation.IsValid)
                {
                    SendError(clientId, $"Token invalide: {validation.ErrorMessage}");
                    continue;
                }

                // Token valide! Traiter la requête
                Console.WriteLine(
                    $"Requête de {validation.Username} " +
                    $"({validation.Role}): {packet.Type}"
                );

                // Traiter selon le type de paquet
                HandleAuthenticatedRequest(packet, validation);
            }

            // ===== TRAITER LA DÉCONNEXION =====
            else if (packet.Type == PacketType.Logout)
            {
                // Révoquer le token
                _authService.RevokeToken(packet.AuthToken);
                
                // Fermer la session
                _sessionManager.CloseSession(clientId);
                
                Console.WriteLine($"✓ Déconnexion: {clientId}");

                var logoutResponse = NetworkPacket.Create(
                    PacketType.Logout,
                    "SERVER",
                    new { success = true, message = "Déconnecté avec succès" }
                );
                
                await SendToClient(clientId, logoutResponse);
                
                break; // Fermer la connexion
            }

            // ===== TRAITER LE RENOUVELLEMENT DE TOKEN =====
            else if (packet.Type == PacketType.RefreshToken)
            {
                var refreshRequest = packet.DeserializePayload<RefreshTokenRequest>();
                
                var refreshResponse = _authService.RefreshToken(
                    refreshRequest,
                    clientId
                );

                if (refreshResponse.Success)
                {
                    // Mettre à jour la session avec le nouveau token
                    _sessionManager.UpdateSessionToken(
                        clientId,
                        refreshResponse.Token
                    );
                }

                var responsePacket = NetworkPacket.Create(
                    PacketType.RefreshToken,
                    "SERVER",
                    refreshResponse
                );
                
                await SendToClient(clientId, responsePacket);
            }
        }
    }
    finally
    {
        _sessionManager.CloseSession(clientId);
        client.Close();
    }
}

// Méthodes helper

private bool RequiresAuthentication(PacketType type) => type switch
{
    PacketType.SystemInfo => true,
    PacketType.ProcessList => true,
    PacketType.Command => true,
    PacketType.Alert => true,
    _ => false
};

private void HandleAuthenticatedRequest(
    NetworkPacket packet, 
    AuthTokenValidation validation)
{
    switch (packet.Type)
    {
        case PacketType.SystemInfo:
            // Traiter la requête de système
            Console.WriteLine($"Traitement SystemInfo pour {validation.Username}");
            break;
            
        case PacketType.ProcessList:
            // Traiter la requête de processus
            Console.WriteLine($"Traitement ProcessList pour {validation.Username}");
            break;
            
        case PacketType.Command:
            // Vérifier les permissions selon le rôle
            if (validation.Role == "Administrator" || 
                validation.Role == "Supervisor")
            {
                Console.WriteLine($"Exécution commande par {validation.Username}");
            }
            else
            {
                Console.WriteLine($"Accès refusé pour {validation.Username}");
            }
            break;
    }
}

private void SendError(string clientId, string message)
{
    var errorPacket = NetworkPacket.Create(
        PacketType.Alert,
        "SERVER",
        new { error = true, message = message }
    );
    
    _ = SendToClient(clientId, errorPacket);
}
```

---

### Exemple 3: Créer un Nouvel Utilisateur Administrativement

```csharp
// Dans Program.cs ou une interface admin
public void CreateNewOperator(
    AuthenticationService authService, 
    string username, 
    string email)
{
    var password = GenerateSecurePassword();
    
    var created = authService.CreateUser(
        username: username,
        password: password,
        email: email,
        fullName: $"Opérateur {username}",
        role: UserRole.Operator
    );

    if (created)
    {
        Console.WriteLine($"✓ Utilisateur {username} créé");
        Console.WriteLine($"  Mot de passe temporaire: {password}");
        Console.WriteLine("  ⚠️ L'utilisateur doit changer ce mot de passe à la première connexion");
    }
    else
    {
        Console.WriteLine($"✗ Impossible de créer {username}");
    }
}

private string GenerateSecurePassword()
{
    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$";
    var random = new Random();
    var password = new string(Enumerable.Range(0, 12)
        .Select(_ => chars[random.Next(chars.Length)])
        .ToArray());
    return password;
}
```

---

## 👤 Exemples Client

### Exemple 1: Implémenter l'Écran de Connexion

```csharp
// Dans MainWindow.xaml.cs
public partial class LoginWindow : Window
{
    private AuthenticationClient _authClient;
    private NetworkClient _networkClient;

    public LoginWindow()
    {
        InitializeComponent();
        InitializeAuthentication();
    }

    private void InitializeAuthentication()
    {
        // Créer le client réseau
        _networkClient = new NetworkClient("127.0.0.1", 5000);
        
        // Créer le client d'authentification
        _authClient = new AuthenticationClient(_networkClient);
        
        // Abonner aux événements
        _authClient.OnAuthenticationChanged += (isAuth) =>
        {
            if (isAuth)
            {
                MessageBox.Show(
                    $"Bienvenue {_authClient.Username}!",
                    "Connexion réussie"
                );
                // Ouvrir la fenêtre principale
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
        };

        _authClient.OnAuthenticationError += (error) =>
        {
            MessageBox.Show(error, "Erreur d'authentification", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            PasswordTextBox.Clear();
        };
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        string username = UsernameTextBox.Text;
        string password = PasswordTextBox.Password;

        if (string.IsNullOrWhiteSpace(username) || 
            string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Veuillez entrer votre identifiant et mot de passe");
            return;
        }

        // Désactiver le bouton pendant l'authentification
        LoginButton.IsEnabled = false;
        LoginButton.Content = "Connexion en cours...";

        try
        {
            // Se connecter au serveur
            await _networkClient.ConnectAsync();
            
            // Envoyer les credentials
            await _authClient.LoginAsync(username, password);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur de connexion: {ex.Message}", "Erreur");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Content = "Connexion";
        }
    }
}
```

### Exemple 2: XAML pour l'Écran de Connexion

```xaml
<Window x:Class="NetAdmin.Client.LoginWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        Title="NetAdmin Pro - Connexion"
        Width="400" Height="300"
        WindowStartupLocation="CenterScreen"
        Background="#F5F5F5">
    <Grid VerticalAlignment="Center" HorizontalAlignment="Center">
        <StackPanel Width="300" Spacing="15">
            
            <!-- Titre -->
            <TextBlock Text="NetAdmin Pro" 
                       FontSize="28" FontWeight="Bold" 
                       Foreground="#1E90FF"
                       HorizontalAlignment="Center"/>
            
            <TextBlock Text="Administration Réseau Sécurisée" 
                       FontSize="12" 
                       Foreground="#666666"
                       HorizontalAlignment="Center"/>
            
            <Separator Background="#CCCCCC" Margin="0,10"/>
            
            <!-- Identifiant -->
            <TextBlock Text="Identifiant" FontWeight="Bold"/>
            <TextBox x:Name="UsernameTextBox"
                     Height="35"
                     Padding="10,8"
                     FontSize="14"
                     Background="White"
                     BorderBrush="#CCCCCC"
                     BorderThickness="1"/>
            
            <!-- Mot de passe -->
            <TextBlock Text="Mot de passe" FontWeight="Bold"/>
            <PasswordBox x:Name="PasswordTextBox"
                         Height="35"
                         Padding="10,8"
                         FontSize="14"
                         Background="White"
                         BorderBrush="#CCCCCC"
                         BorderThickness="1"/>
            
            <!-- Bouton Connexion -->
            <Button x:Name="LoginButton"
                    Content="Connexion"
                    Height="40"
                    Background="#1E90FF"
                    Foreground="White"
                    FontWeight="Bold"
                    FontSize="14"
                    Click="LoginButton_Click"
                    Cursor="Hand"/>
            
            <!-- Lien Aide -->
            <TextBlock HorizontalAlignment="Center">
                <Hyperlink Foreground="#1E90FF">
                    Mot de passe oublié?
                </Hyperlink>
            </TextBlock>
        </StackPanel>
    </Grid>
</Window>
```

### Exemple 3: Envoyer une Requête Authentifiée

```csharp
// Dans NetworkClient.cs
public async Task RequestSystemInfoAsync(AuthenticationClient authClient)
{
    try
    {
        // Créer la requête
        var systemInfo = new { osVersion = "Windows 10", ram = "16GB" };
        var packet = NetworkPacket.Create(
            PacketType.SystemInfo,
            Environment.MachineName,
            systemInfo
        );

        // Ajouter le token d'authentification
        packet = authClient.AuthorizePacket(packet);

        // Envoyer
        await SendPacket(packet);

        Console.WriteLine("✓ Requête SystemInfo envoyée avec authentification");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Erreur: {ex.Message}");
    }
}
```

---

## ⚠️ Gestion des Erreurs

### Exemple 1: Gérer les Erreurs d'Authentification

```csharp
private async Task SafeAuthenticateAsync(
    AuthenticationClient authClient,
    string username,
    string password)
{
    try
    {
        // Validation basique
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            Console.WriteLine("Erreur: Identifiant et mot de passe requis");
            return;
        }

        // Essayer de se connecter
        bool success = await authClient.LoginAsync(username, password);

        if (!success)
        {
            Console.WriteLine("Erreur: Authentification échouée");
        }
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Erreur: Connexion au serveur dépassée");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"Erreur: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur inattendue: {ex.Message}");
    }
}
```

### Exemple 2: Renouvellement de Token Automatique

```csharp
private async Task AutoRefreshTokenLoop(AuthenticationClient authClient)
{
    while (authClient.IsAuthenticated)
    {
        try
        {
            // Vérifier si refresh nécessaire
            if (!authClient.IsTokenValid())
            {
                Console.WriteLine("🔄 Renouvellement du token...");
                
                bool refreshed = await authClient.RefreshTokenAsync();
                
                if (refreshed)
                {
                    Console.WriteLine("✓ Token renouvelé");
                }
                else
                {
                    Console.WriteLine("✗ Renouvellement échoué - Se reconnecter");
                    await authClient.LogoutAsync();
                    break;
                }
            }

            // Attendre 5 minutes avant de revérifier
            await Task.Delay(TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur during token refresh: {ex.Message}");
        }
    }
}
```

---

## 🧪 Tests Unitaires

### Exemple 1: Tester l'Authentification

```csharp
[TestClass]
public class AuthenticationServiceTests
{
    private AppDbContext _context;
    private AuthenticationService _authService;

    [TestInitialize]
    public void Setup()
    {
        // Utiliser une BD en mémoire pour les tests
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("TestDb")
            .Options;

        _context = new AppDbContext();
        _authService = new AuthenticationService(
            _context,
            "test-secret-key-min-32-characters!!!",
            60,
            7
        );

        // Créer un utilisateur test
        _authService.CreateUser(
            "testuser",
            "TestPassword@123",
            "test@example.com",
            "Test User",
            UserRole.Operator
        );
    }

    [TestMethod]
    public void TestValidLogin()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "TestPassword@123"
        };

        // Act
        var response = _authService.Authenticate(request, "127.0.0.1");

        // Assert
        Assert.IsTrue(response.Success);
        Assert.IsNotNull(response.Token);
        Assert.AreEqual("testuser", response.Username);
    }

    [TestMethod]
    public void TestInvalidPassword()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "WrongPassword"
        };

        // Act
        var response = _authService.Authenticate(request, "127.0.0.1");

        // Assert
        Assert.IsFalse(response.Success);
        Assert.IsNull(response.Token);
    }

    [TestMethod]
    public void TestTokenValidation()
    {
        // Arrange
        var loginResponse = _authService.Authenticate(
            new LoginRequest { Username = "testuser", Password = "TestPassword@123" },
            "127.0.0.1"
        );

        // Act
        var validation = _authService.ValidateToken(loginResponse.Token);

        // Assert
        Assert.IsTrue(validation.IsValid);
        Assert.AreEqual("testuser", validation.Username);
    }
}
```

---

## ⚙️ Configuration

### Exemple 1: Changer les Paramètres JWT

```json
{
  "JwtSettings": {
    "Secret": "votre-secret-super-securise-de-32-caracteres-minimum!!!!",
    "TokenExpirationMinutes": 120,
    "RefreshTokenExpirationDays": 14
  }
}
```

### Exemple 2: Configuration Environnement

```csharp
// Dans Program.cs
var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
    == "Development";

var jwtSecret = isDevelopment 
    ? "dev-secret-key-change-in-production"
    : Environment.GetEnvironmentVariable("JWT_SECRET");

if (string.IsNullOrEmpty(jwtSecret))
{
    throw new InvalidOperationException("JWT_SECRET not configured!");
}

var authService = new AuthenticationService(
    context,
    jwtSecret,
    tokenExpirationMinutes: 60,
    refreshTokenExpirationDays: 7
);
```

---

## 📚 Ressources Supplémentaires

- Documentation: Voir les fichiers .md
- Code Source: Consulter les services
- Tests: Voir AuthenticationTester.cs

---

**Créé:** 4 Février 2026  
**Auteur:** GitHub Copilot  
**Prêt pour:** Développement & Production (après sécurisation)
