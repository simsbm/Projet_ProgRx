# NetAdmin

## Description du Projet

NetAdmin est une application client-serveur développée en .NET, conçue pour la surveillance et l'administration de systèmes à distance. Le projet se compose d'une application cliente légère qui collecte des informations système (matériel et processus) et les transmet à un serveur central. Le serveur, doté d'une interface utilisateur graphique (WPF), permet la gestion des clients connectés, l'affichage des données collectées et l'exécution de commandes à distance.

Ce projet met en œuvre une architecture modulaire, une communication réseau sécurisée, et une gestion des données persistante, le tout dans un environnement .NET.

## Fonctionnalités

*   **Surveillance Matérielle :** Collecte en temps réel de l'utilisation du CPU, de la mémoire RAM disponible et totale, ainsi que des informations sur le système d'exploitation du client.
*   **Surveillance des Processus :** Récupération de la liste des processus en cours d'exécution sur la machine cliente.
*   **Communication Client-Serveur :** Établissement d'une connexion TCP/IP sécurisée entre le client et le serveur pour l'échange de données et de commandes.
*   **Authentification Utilisateur :** Système d'authentification robuste basé sur JWT (JSON Web Tokens) et hachage des mots de passe avec BCrypt.Net-Next.
*   **Interface Utilisateur Graphique (WPF) :** Tableau de bord intuitif sur le serveur pour visualiser les données des clients, gérer les utilisateurs et interagir avec les machines distantes.
*   **Persistance des Données :** Utilisation de SQLite via Entity Framework Core pour stocker les informations utilisateur et les données de configuration.
*   **Visualisation des Données :** Intégration de LiveCharts.Wpf pour des représentations graphiques claires des métriques système.
*   **Design Moderne :** Interface utilisateur améliorée avec MaterialDesignThemes.

## Technologies Utilisées

Le projet est développé en C# et utilise le framework .NET 10.0.

### Côté Serveur (`NetAdmin.Server`)

*   **Framework :** .NET 10.0 (Windows Desktop)
*   **Interface Utilisateur :** WPF (Windows Presentation Foundation)
*   **Base de Données :** SQLite avec Entity Framework Core
*   **Authentification :** JWT (JSON Web Tokens), BCrypt.Net-Next
*   **Graphiques :** LiveCharts.Wpf
*   **Composants UI :** MaterialDesignThemes

### Côté Client (`NetAdmin.Client`)

*   **Framework :** .NET 10.0
*   **Collecte de Données :** `System.Diagnostics.PerformanceCounter`, `System.Management`

### Partagé (`NetAdmin.Shared`)

*   **Framework :** .NET 10.0
*   **Modèles de Données :** Classes partagées pour les informations matérielles, les processus, les paquets réseau, etc.
*   **Protocoles de Communication :** Définitions des types de commandes et de paquets.

## Structure du Projet

Le dépôt est organisé en trois projets principaux :

*   `NetAdmin.Client/` : Contient le code source de l'application cliente. Ce projet est responsable de la collecte des données système et de leur envoi au serveur.
*   `NetAdmin.Server/` : Contient le code source de l'application serveur. Il inclut l'interface utilisateur WPF, la logique de gestion des clients, l'authentification et l'accès à la base de données.
*   `NetAdmin.Shared/` : Une bibliothèque de classes partagée qui définit les modèles de données et les types de communication utilisés par les applications cliente et serveur.

## Installation

Pour installer et exécuter le projet NetAdmin, suivez les étapes ci-dessous :

1.  **Prérequis :**
    *   Assurez-vous d'avoir le SDK .NET 10.0 (ou une version compatible) installé sur votre machine.
    *   Un environnement de développement tel que Visual Studio ou Visual Studio Code avec les extensions C# est recommandé.

2.  **Cloner le dépôt :**
    ```bash
    git clone https://github.com/simsbm/Projet_ProgRx.git
    cd Projet_ProgRx
    ```

3.  **Restaurer les dépendances et compiler :**
    Ouvrez la solution (`Projet_ProgRx.sln`) dans votre IDE ou utilisez la ligne de commande :
    ```bash
    dotnet restore
    dotnet build
    ```

## Utilisation

### Démarrage du Serveur

1.  Naviguez vers le répertoire du projet serveur :
    ```bash
    cd NetAdmin.Server
    ```
2.  Exécutez l'application serveur :
    ```bash
    dotnet run
    ```
    L'interface utilisateur WPF du serveur devrait se lancer. Lors du premier démarrage, la base de données SQLite sera initialisée et un utilisateur administrateur par défaut pourrait être créé (vérifiez `NetAdmin.Server/Data/DatabaseInitializer.cs` pour les détails).

### Démarrage du Client

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

## Auteurs

*  **NGONO NGONO CECILE**
*   **NKWAWYA JOEL** 
*   **PIANTA PINDO CHIRANEL**
*   **SANGUEN JORDAN** 
*   **SIMO WAFFO BREL MOREL**
  

