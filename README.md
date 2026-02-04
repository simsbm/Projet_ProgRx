# Documentation NetAdmin Pro

## Table des matieres

1. Presentation
2. Architecture generale
3. Modules et responsabilites
4. Installation et prerequis
5. Configuration
6. Demarrage
7. Authentification et securite
8. Base de donnees
9. Supervision et UI
10. Notifications Discord
11. Depannage

## 1. Presentation

NetAdmin Pro est une application client-serveur en .NET pour la surveillance et l'administration a distance.  
Le client collecte des informations systeme (materiel, processus, etat) et les envoie au serveur.  
Le serveur met a disposition une interface WPF pour visualiser les donnees, suivre les clients et executer des actions.
## Installation

Pour installer et exécuter le projet NetAdmin, suivez les étapes ci-dessous :
 **Prérequis :**
    *   Assurez-vous d'avoir le SDK .NET 10.0 (ou une version compatible) installé sur votre machine.
    *   Un environnement de développement tel que Visual Studio ou Visual Studio Code avec les extensions C# est recommandé.

**Cloner le dépôt :**
    ```bash
    git clone https://github.com/simsbm/Projet_ProgRx.git
    cd Projet_ProgRx
    ```

## 2. Architecture generale

Le projet est compose de trois projets .NET 10 :
- `NetAdmin.Client` : agent de collecte et communication.
- `NetAdmin.Server` : application WPF, services, persistance.
- `NetAdmin.Shared` : modeles et protocoles partages.

Flux simplifie :
1. Le client se connecte au serveur TCP.
2. Il envoie des paquets reseau (infos systeme, processus, etc.).
3. Le serveur traite, enregistre en base et met a jour l'UI.
4. Des actions peuvent etre executees depuis le serveur vers le client.

## 3. Modules et responsabilites

- **Client**
  - Collecte CPU/RAM/processus.
  - Envoi de donnees au serveur via TCP.
  - Gestion de configuration locale.

- **Server**
  - Serveur TCP et orchestration des clients.
  - Interface WPF (graphiques, tableaux, logs).
  - Authentification et gestion des sessions.
  - Persistance SQLite via EF Core.

- **Shared**
  - DTOs et definitions de paquets reseau.
  - Types utilises des deux cotes.

## 4. Installation et prerequis

Prerequis :
- Windows 10/11 (serveur WPF).
- .NET SDK 10.0.
- IDE recommande : Visual Studio ou VS Code.

Installation :
```bash
dotnet restore
dotnet build
```

## 5. Configuration

### Serveur
Fichier : `NetAdmin.Server/appsettings.json`
```json
{
  "JwtSettings": {
    "Secret": "your-super-secret-key-min-32-characters-for-security",
    "TokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 7
  },
  "Database": {
    "ConnectionString": "Data Source=netadmin.db"
  },
  "Server": {
    "Port": 5000,
    "MaxConnections": 100
  },
  "Discord": {
    "Enabled": false,
    "WebhookUrl": "",
    "Username": "NetAdmin Pro",
    "AvatarUrl": "",
    "ServerName": ""
  }
}
```

### Client
Fichier : `NetAdmin.Client/appsettings.json`
```json
{
  "Server": {
    "Host": "127.0.0.1",
    "Port": 5000
  },
  "Authentication": {
    "AutoRefreshToken": true,
    "RefreshIntervalMinutes": 55
  },
  "Client": {
    "HeartbeatIntervalSeconds": 30,
    "ConnectTimeoutSeconds": 10
  }
}
```

## 6. Demarrage

### Serveur
```bash
cd NetAdmin.Server
dotnet run
```

### Client
1.  Naviguez vers le répertoire du projet client :
    ```bash
    cd NetAdmin.Client
    ```
2.  Exécutez l'application cliente en spécifiant l'adresse IP et le port du serveur. Vous pouvez les fournir via les arguments de ligne de commande ou via le fichier `appsettings.json` (ou saisie manuelle si non configuré) :
    ```bash
    dotnet run <adresse_ip_serveur> <port_serveur>
    # Exemple :
    # dotnet run 127.0.0.1 5000
    ```
    Le client tentera de se connecter au serveur et commencera à envoyer les données collectées.

## 7. Authentification et securite

Mecanismes implementes :
- Hachage des mots de passe via BCrypt (salt automatique).
- JWT signe HMAC-SHA256 (claims : userId, username, email, role, fullName).
- Refresh tokens en base, revocation possible.
- Verification serveur des tokens (revokes, expirations).
- Session manager cote serveur (role, IP, activite).
- Delai sur echec de login pour limiter le brute force.

Identifiants par defaut (a changer en production) :
```text
admin / Admin@123!
supervisor / Supervisor@123!
operator / Operator@123!
viewer / Viewer@123!
```

## 8. Base de donnees

La base est en SQLite, geree par EF Core.  
Tables principales :
- `Users`
- `AuthTokens`
- `AuditLogs`
- `ClientHosts`
- `MetricLogs`

Fichiers utiles :
- `ARCHITECTURE_DATABASE.md`
- `DATABASE_CONFIGURATION.md`
- `DATABASE_TEST_GUIDE.md`

## 9. Supervision et UI

Le serveur fournit :
- Statistiques globales (clients en ligne, dernier client).
- Graphiques temps reel (CPU/RAM).
- Tableau des machines connectees.
- Journal de logs serveur.

## 10. Notifications Discord

Le serveur peut envoyer des notifications via webhook.

Configuration :
1. Creer un webhook dans Discord.
2. Renseigner `Discord.WebhookUrl`.
3. Passer `Discord.Enabled` a `true`.

Evenements notifies :
- Demarrage serveur.
- Connexion client.
- Enregistrement client.

## 11. Depannage

- Logs de demarrage : `netadmin.startup.log`.
- Verifier le port TCP (par defaut 5000).
- Verifier les secrets JWT dans `appsettings.json`.

## Auteurs
*   **NGONO NGONO CECILE**
*   **NKWAWYA JOEL** 
*   **PIANTA PINDO CHIRANEL**
*   **SANGUEN JORDAN** 
*   **SIMO WAFFO BREL MOREL** 


