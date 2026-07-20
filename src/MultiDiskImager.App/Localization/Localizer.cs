using System.Globalization;
using Avalonia.Markup.Xaml;

namespace MultiDiskImager.Localization;

public static class Localizer
{
    private static readonly string[] Keys = Lines("""
        Tagline
        Settings
        About
        UpdateAvailable
        ViewRelease
        RawImage
        ImagePlaceholder
        Browse
        NoMetadata
        Devices
        Refresh
        Info
        Options
        OnlyAllocated
        VerifyAfter
        ImageChecksum
        Checksum
        Calculate
        Cancel
        ReadDevice
        WriteImage
        Verify
        QuickWipe
        General
        WriteWarnings
        Animations
        UpdateChecks
        SoundNotification
        AutoSelect
        AutoClose
        AlwaysOnTop
        FilePicker
        UseCustomFolder
        FolderPath
        CustomPlaces
        DeviceFilters
        ShowExternal
        HideLarger
        Appearance
        Theme
        System
        Light
        Dark
        TitleExtra
        Nothing
        Percent
        CurrentSpeed
        RemainingTime
        ActiveDevice
        ImageFileName
        Save
        Continue
        Close
        AboutTitle
        AboutDescription
        Author
        """);

    private static readonly IReadOnlyDictionary<string, string[]> Catalogs = new Dictionary<string, string[]>
    {
        ["en"] = Lines("""
            Write one raw image to multiple disks — safely and in parallel
            Settings
            About
            A newer version is available from GitHub Releases.
            View release
            Raw image
            Choose or drop a byte-for-byte .img file
            Browse…
            No compression, encryption, headers, or proprietary metadata are added.
            Devices
            Refresh
            Info
            Options
            Read through last allocated partition
            Verify data when finished
            Image checksum
            Checksum
            Calculate
            Cancel
            Read device
            Write image
            Verify
            Quick wipe
            General
            Display warnings before write and wipe
            Enable animations
            Check GitHub Releases for updates on startup
            Play a notification sound when finished
            Automatically select a single eligible device
            Close after a fully successful operation
            Keep the main window on top
            File picker
            Start in a user-specified folder
            Folder path
            Custom places (one folder per line)
            Device safety filters
            Show external hard drives (system disk is always hidden)
            Hide devices larger than
            Appearance
            Theme
            System
            Light
            Dark
            Title extra
            Nothing
            Percent
            Current speed
            Remaining time
            Active device
            Image file name
            Save
            Continue
            Close
            About bNovate Multi Disk Imager
            A professional cross-platform raw disk imaging utility. Images remain byte-for-byte data without proprietary metadata.
            Author
            """),
        ["fr"] = Lines("""
            Écrivez une image brute sur plusieurs disques — en toute sécurité et en parallèle
            Paramètres
            À propos
            Une nouvelle version est disponible sur GitHub Releases.
            Voir la version
            Image brute
            Choisissez ou déposez un fichier .img identique octet par octet
            Parcourir…
            Aucune compression, chiffrement, en-tête ou métadonnée propriétaire n’est ajouté.
            Périphériques
            Actualiser
            Infos
            Options
            Lire jusqu’à la dernière partition allouée
            Vérifier les données à la fin
            Somme de contrôle de l’image
            Somme de contrôle
            Calculer
            Annuler
            Lire le périphérique
            Écrire l’image
            Vérifier
            Effacement rapide
            Général
            Afficher les avertissements avant l’écriture et l’effacement
            Activer les animations
            Rechercher les mises à jour GitHub au démarrage
            Émettre un son à la fin
            Sélectionner automatiquement l’unique périphérique admissible
            Fermer après une opération entièrement réussie
            Garder la fenêtre principale au premier plan
            Sélecteur de fichiers
            Démarrer dans un dossier défini par l’utilisateur
            Chemin du dossier
            Emplacements personnalisés (un dossier par ligne)
            Filtres de sécurité des périphériques
            Afficher les disques durs externes (le disque système reste masqué)
            Masquer les périphériques de plus de
            Apparence
            Thème
            Système
            Clair
            Sombre
            Complément du titre
            Aucun
            Pourcentage
            Vitesse actuelle
            Temps restant
            Périphérique actif
            Nom du fichier image
            Enregistrer
            Continuer
            Fermer
            À propos de bNovate Multi Disk Imager
            Un utilitaire professionnel et multiplateforme d’imagerie disque brute. Les images restent des données octet par octet sans métadonnées propriétaires.
            Auteur
            """),
        ["de"] = Lines("""
            Ein Rohabbild sicher und parallel auf mehrere Datenträger schreiben
            Einstellungen
            Über
            Eine neuere Version ist auf GitHub Releases verfügbar.
            Version anzeigen
            Rohabbild
            Eine bytegenaue .img-Datei auswählen oder ablegen
            Durchsuchen…
            Es werden keine Komprimierung, Verschlüsselung, Header oder proprietären Metadaten hinzugefügt.
            Datenträger
            Aktualisieren
            Info
            Optionen
            Nur bis zur letzten belegten Partition lesen
            Daten nach Abschluss überprüfen
            Abbild-Prüfsumme
            Prüfsumme
            Berechnen
            Abbrechen
            Datenträger lesen
            Abbild schreiben
            Überprüfen
            Schnell löschen
            Allgemein
            Vor Schreiben und Löschen warnen
            Animationen aktivieren
            Beim Start auf GitHub nach Updates suchen
            Nach Abschluss einen Ton abspielen
            Einen einzelnen geeigneten Datenträger automatisch auswählen
            Nach vollständig erfolgreichem Vorgang schließen
            Hauptfenster im Vordergrund halten
            Dateiauswahl
            In einem benutzerdefinierten Ordner starten
            Ordnerpfad
            Eigene Orte (ein Ordner pro Zeile)
            Sicherheitsfilter für Datenträger
            Externe Festplatten anzeigen (Systemdatenträger bleibt verborgen)
            Größere Datenträger ausblenden als
            Darstellung
            Design
            System
            Hell
            Dunkel
            Titelzusatz
            Nichts
            Prozent
            Aktuelle Geschwindigkeit
            Verbleibende Zeit
            Aktiver Datenträger
            Name der Abbilddatei
            Speichern
            Fortfahren
            Schließen
            Über bNovate Multi Disk Imager
            Ein professionelles plattformübergreifendes Dienstprogramm für Rohabbilder. Abbilder bleiben bytegenau und frei von proprietären Metadaten.
            Autor
            """),
        ["it"] = Lines("""
            Scrivi un’immagine raw su più dischi — in sicurezza e in parallelo
            Impostazioni
            Informazioni
            È disponibile una nuova versione su GitHub Releases.
            Visualizza versione
            Immagine raw
            Scegli o trascina un file .img identico byte per byte
            Sfoglia…
            Non vengono aggiunti compressione, crittografia, intestazioni o metadati proprietari.
            Dispositivi
            Aggiorna
            Info
            Opzioni
            Leggi fino all’ultima partizione allocata
            Verifica i dati al termine
            Checksum immagine
            Checksum
            Calcola
            Annulla
            Leggi dispositivo
            Scrivi immagine
            Verifica
            Cancellazione rapida
            Generale
            Mostra avvisi prima di scrittura e cancellazione
            Abilita animazioni
            Controlla gli aggiornamenti GitHub all’avvio
            Riproduci un suono al termine
            Seleziona automaticamente l’unico dispositivo idoneo
            Chiudi dopo un’operazione completamente riuscita
            Mantieni la finestra principale in primo piano
            Selettore file
            Inizia in una cartella specificata dall’utente
            Percorso cartella
            Percorsi personalizzati (una cartella per riga)
            Filtri di sicurezza dei dispositivi
            Mostra dischi rigidi esterni (il disco di sistema resta nascosto)
            Nascondi dispositivi più grandi di
            Aspetto
            Tema
            Sistema
            Chiaro
            Scuro
            Extra nel titolo
            Nulla
            Percentuale
            Velocità attuale
            Tempo rimanente
            Dispositivo attivo
            Nome file immagine
            Salva
            Continua
            Chiudi
            Informazioni su bNovate Multi Disk Imager
            Un’utilità professionale multipiattaforma per immagini disco raw. Le immagini restano dati identici byte per byte senza metadati proprietari.
            Autore
            """),
        ["es"] = Lines("""
            Escriba una imagen sin procesar en varios discos — de forma segura y en paralelo
            Configuración
            Acerca de
            Hay una versión nueva disponible en GitHub Releases.
            Ver versión
            Imagen sin procesar
            Elija o suelte un archivo .img idéntico byte por byte
            Examinar…
            No se añaden compresión, cifrado, cabeceras ni metadatos propietarios.
            Dispositivos
            Actualizar
            Información
            Opciones
            Leer hasta la última partición asignada
            Verificar los datos al finalizar
            Suma de comprobación de imagen
            Suma de comprobación
            Calcular
            Cancelar
            Leer dispositivo
            Escribir imagen
            Verificar
            Borrado rápido
            General
            Mostrar avisos antes de escribir y borrar
            Activar animaciones
            Buscar actualizaciones de GitHub al iniciar
            Reproducir un sonido al finalizar
            Seleccionar automáticamente el único dispositivo apto
            Cerrar tras una operación totalmente correcta
            Mantener la ventana principal encima
            Selector de archivos
            Iniciar en una carpeta especificada por el usuario
            Ruta de carpeta
            Ubicaciones personalizadas (una carpeta por línea)
            Filtros de seguridad de dispositivos
            Mostrar discos duros externos (el disco del sistema siempre se oculta)
            Ocultar dispositivos mayores de
            Apariencia
            Tema
            Sistema
            Claro
            Oscuro
            Información adicional del título
            Nada
            Porcentaje
            Velocidad actual
            Tiempo restante
            Dispositivo activo
            Nombre del archivo de imagen
            Guardar
            Continuar
            Cerrar
            Acerca de bNovate Multi Disk Imager
            Una utilidad profesional multiplataforma para imágenes de disco sin procesar. Las imágenes conservan los datos byte por byte sin metadatos propietarios.
            Autor
            """),
        ["pt"] = Lines("""
            Grave uma imagem bruta em vários discos — com segurança e em paralelo
            Definições
            Acerca de
            Está disponível uma versão mais recente no GitHub Releases.
            Ver versão
            Imagem bruta
            Escolha ou largue um ficheiro .img idêntico byte a byte
            Procurar…
            Não são adicionados compressão, encriptação, cabeçalhos ou metadados proprietários.
            Dispositivos
            Atualizar
            Informação
            Opções
            Ler até à última partição alocada
            Verificar os dados no final
            Soma de verificação da imagem
            Soma de verificação
            Calcular
            Cancelar
            Ler dispositivo
            Gravar imagem
            Verificar
            Limpeza rápida
            Geral
            Mostrar avisos antes de gravar e limpar
            Ativar animações
            Procurar atualizações no GitHub ao iniciar
            Reproduzir um som ao terminar
            Selecionar automaticamente o único dispositivo elegível
            Fechar após uma operação totalmente bem-sucedida
            Manter a janela principal no topo
            Seletor de ficheiros
            Iniciar numa pasta definida pelo utilizador
            Caminho da pasta
            Locais personalizados (uma pasta por linha)
            Filtros de segurança dos dispositivos
            Mostrar discos rígidos externos (o disco do sistema fica sempre oculto)
            Ocultar dispositivos maiores que
            Aspeto
            Tema
            Sistema
            Claro
            Escuro
            Extra do título
            Nada
            Percentagem
            Velocidade atual
            Tempo restante
            Dispositivo ativo
            Nome do ficheiro de imagem
            Guardar
            Continuar
            Fechar
            Acerca do bNovate Multi Disk Imager
            Um utilitário profissional multiplataforma para imagens de disco brutas. As imagens mantêm os dados byte a byte sem metadados proprietários.
            Autor
            """),
        ["nl"] = Lines("""
            Schrijf één onbewerkt beeld veilig en parallel naar meerdere schijven
            Instellingen
            Over
            Er is een nieuwere versie beschikbaar op GitHub Releases.
            Versie bekijken
            Onbewerkt beeld
            Kies of sleep een byte-voor-byte identiek .img-bestand
            Bladeren…
            Er worden geen compressie, versleuteling, headers of bedrijfseigen metadata toegevoegd.
            Apparaten
            Vernieuwen
            Info
            Opties
            Lezen tot en met de laatste toegewezen partitie
            Gegevens na afloop verifiëren
            Controlesom van beeld
            Controlesom
            Berekenen
            Annuleren
            Apparaat lezen
            Beeld schrijven
            Verifiëren
            Snel wissen
            Algemeen
            Waarschuwingen tonen vóór schrijven en wissen
            Animaties inschakelen
            Bij opstarten op GitHub naar updates zoeken
            Een geluid afspelen wanneer voltooid
            Eén geschikt apparaat automatisch selecteren
            Sluiten na een volledig geslaagde bewerking
            Hoofdvenster op de voorgrond houden
            Bestandskiezer
            Starten in een door de gebruiker opgegeven map
            Mappad
            Aangepaste locaties (één map per regel)
            Veiligheidsfilters voor apparaten
            Externe harde schijven tonen (systeemschijf blijft verborgen)
            Apparaten verbergen groter dan
            Weergave
            Thema
            Systeem
            Licht
            Donker
            Extra titeltekst
            Niets
            Percentage
            Huidige snelheid
            Resterende tijd
            Actief apparaat
            Naam van beeldbestand
            Opslaan
            Doorgaan
            Sluiten
            Over bNovate Multi Disk Imager
            Een professioneel platformonafhankelijk hulpprogramma voor onbewerkte schijfbeelden. Beelden blijven byte-voor-byte gegevens zonder bedrijfseigen metadata.
            Auteur
            """),
        ["pl"] = Lines("""
            Zapisuj jeden surowy obraz na wiele dysków — bezpiecznie i równolegle
            Ustawienia
            Informacje
            Nowsza wersja jest dostępna w GitHub Releases.
            Zobacz wersję
            Surowy obraz
            Wybierz lub upuść identyczny bajt w bajt plik .img
            Przeglądaj…
            Nie są dodawane kompresja, szyfrowanie, nagłówki ani własnościowe metadane.
            Urządzenia
            Odśwież
            Informacje
            Opcje
            Czytaj do ostatniej przydzielonej partycji
            Zweryfikuj dane po zakończeniu
            Suma kontrolna obrazu
            Suma kontrolna
            Oblicz
            Anuluj
            Odczytaj urządzenie
            Zapisz obraz
            Zweryfikuj
            Szybkie czyszczenie
            Ogólne
            Pokazuj ostrzeżenia przed zapisem i czyszczeniem
            Włącz animacje
            Sprawdzaj aktualizacje GitHub przy uruchomieniu
            Odtwórz dźwięk po zakończeniu
            Automatycznie wybierz jedyne odpowiednie urządzenie
            Zamknij po całkowicie udanej operacji
            Utrzymuj główne okno na wierzchu
            Wybór pliku
            Rozpocznij w folderze wskazanym przez użytkownika
            Ścieżka folderu
            Własne lokalizacje (jeden folder w wierszu)
            Filtry bezpieczeństwa urządzeń
            Pokaż zewnętrzne dyski twarde (dysk systemowy jest zawsze ukryty)
            Ukryj urządzenia większe niż
            Wygląd
            Motyw
            System
            Jasny
            Ciemny
            Dodatek do tytułu
            Brak
            Procent
            Bieżąca prędkość
            Pozostały czas
            Aktywne urządzenie
            Nazwa pliku obrazu
            Zapisz
            Kontynuuj
            Zamknij
            Informacje o bNovate Multi Disk Imager
            Profesjonalne, wieloplatformowe narzędzie do surowych obrazów dysków. Obrazy pozostają danymi bajt w bajt bez własnościowych metadanych.
            Autor
            """),
        ["zh"] = Lines("""
            安全并行地将一个原始映像写入多个磁盘
            设置
            关于
            GitHub Releases 上有新版本可用。
            查看版本
            原始映像
            选择或拖放逐字节一致的 .img 文件
            浏览…
            不添加压缩、加密、标头或专有元数据。
            设备
            刷新
            信息
            选项
            读取到最后一个已分配分区
            完成后验证数据
            映像校验和
            校验和
            计算
            取消
            读取设备
            写入映像
            验证
            快速擦除
            常规
            写入和擦除前显示警告
            启用动画
            启动时检查 GitHub 更新
            完成时播放提示音
            自动选择唯一符合条件的设备
            操作完全成功后关闭
            保持主窗口置顶
            文件选择器
            从用户指定的文件夹开始
            文件夹路径
            自定义位置（每行一个文件夹）
            设备安全筛选器
            显示外部硬盘（系统盘始终隐藏）
            隐藏大于此容量的设备
            外观
            主题
            系统
            浅色
            深色
            标题附加信息
            无
            百分比
            当前速度
            剩余时间
            活动设备
            映像文件名
            保存
            继续
            关闭
            关于 bNovate Multi Disk Imager
            专业的跨平台原始磁盘映像工具。映像保持逐字节数据，不含专有元数据。
            作者
            """),
        ["ja"] = Lines("""
            1つのRAWイメージを複数のディスクへ安全に並列書き込み
            設定
            バージョン情報
            GitHub Releasesに新しいバージョンがあります。
            リリースを表示
            RAWイメージ
            バイト単位で同一の.imgファイルを選択またはドロップ
            参照…
            圧縮、暗号化、ヘッダー、独自メタデータは追加されません。
            デバイス
            更新
            情報
            オプション
            最後に割り当てられたパーティションまで読み取る
            完了後にデータを検証
            イメージのチェックサム
            チェックサム
            計算
            キャンセル
            デバイスを読み取る
            イメージを書き込む
            検証
            クイック消去
            全般
            書き込みと消去の前に警告を表示
            アニメーションを有効にする
            起動時にGitHubの更新を確認
            完了時に通知音を再生
            対象デバイスが1台の場合は自動選択
            完全に成功した操作後に閉じる
            メインウィンドウを常に手前に表示
            ファイル選択
            ユーザー指定フォルダーから開始
            フォルダーのパス
            カスタム場所（1行に1フォルダー）
            デバイス安全フィルター
            外付けハードディスクを表示（システムディスクは常に非表示）
            次の容量を超えるデバイスを非表示
            外観
            テーマ
            システム
            ライト
            ダーク
            タイトルの追加表示
            なし
            パーセント
            現在の速度
            残り時間
            アクティブなデバイス
            イメージファイル名
            保存
            続行
            閉じる
            bNovate Multi Disk Imagerについて
            プロフェッショナルなクロスプラットフォームRAWディスクイメージツール。イメージは独自メタデータなしのバイト単位データを維持します。
            作成者
            """),
    };

    private static readonly IReadOnlyDictionary<string, int> KeyIndexes = Keys
        .Select((key, index) => (key, index))
        .ToDictionary(item => item.key, item => item.index, StringComparer.Ordinal);

    private static readonly string[] AdditionalKeys =
    [
        "Language", "SystemDefault", "LanguageRestart", "Ready", "RefreshingDevices", "NoEligibleDevices",
        "UnsupportedPlatform", "Preparing", "SelectAtLeastOneDevice", "OperationComplete", "OperationResults",
        "Success", "Failed", "SelectFolder", "ChecksumFailed", "CancelActiveOperation"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> AdditionalCatalogs = new Dictionary<string, string[]>
    {
        ["en"] = Extra("Language|System default|Language changes apply after restart.|Ready|Refreshing devices…|No eligible removable devices found|This platform is not supported|Preparing…|Select at least one device.|Operation complete|Operation results|Success|Failed|Select folder|Checksum failed|Cancel active operation?"),
        ["fr"] = Extra("Langue|Langue du système|Le changement de langue s’applique après le redémarrage.|Prêt|Actualisation des périphériques…|Aucun périphérique amovible admissible trouvé|Cette plateforme n’est pas prise en charge|Préparation…|Sélectionnez au moins un périphérique.|Opération terminée|Résultats de l’opération|Réussite|Échec|Sélectionner un dossier|Échec de la somme de contrôle|Annuler l’opération en cours ?"),
        ["de"] = Extra("Sprache|Systemsprache|Sprachänderungen gelten nach einem Neustart.|Bereit|Datenträger werden aktualisiert…|Keine geeigneten Wechseldatenträger gefunden|Diese Plattform wird nicht unterstützt|Vorbereitung…|Mindestens einen Datenträger auswählen.|Vorgang abgeschlossen|Vorgangsergebnisse|Erfolgreich|Fehlgeschlagen|Ordner auswählen|Prüfsumme fehlgeschlagen|Aktiven Vorgang abbrechen?"),
        ["it"] = Extra("Lingua|Predefinita di sistema|Le modifiche della lingua si applicano dopo il riavvio.|Pronto|Aggiornamento dispositivi…|Nessun dispositivo rimovibile idoneo trovato|Questa piattaforma non è supportata|Preparazione…|Selezionare almeno un dispositivo.|Operazione completata|Risultati dell’operazione|Riuscito|Non riuscito|Seleziona cartella|Checksum non riuscito|Annullare l’operazione attiva?"),
        ["es"] = Extra("Idioma|Predeterminado del sistema|Los cambios de idioma se aplican tras reiniciar.|Listo|Actualizando dispositivos…|No se encontraron dispositivos extraíbles aptos|Esta plataforma no es compatible|Preparando…|Seleccione al menos un dispositivo.|Operación completada|Resultados de la operación|Correcto|Error|Seleccionar carpeta|Error de suma de comprobación|¿Cancelar la operación activa?"),
        ["pt"] = Extra("Idioma|Predefinição do sistema|As alterações de idioma são aplicadas após reiniciar.|Pronto|A atualizar dispositivos…|Não foram encontrados dispositivos amovíveis elegíveis|Esta plataforma não é suportada|A preparar…|Selecione pelo menos um dispositivo.|Operação concluída|Resultados da operação|Sucesso|Falhou|Selecionar pasta|Falha na soma de verificação|Cancelar a operação ativa?"),
        ["nl"] = Extra("Taal|Systeemstandaard|Taalwijzigingen gelden na opnieuw starten.|Gereed|Apparaten vernieuwen…|Geen geschikte verwisselbare apparaten gevonden|Dit platform wordt niet ondersteund|Voorbereiden…|Selecteer ten minste één apparaat.|Bewerking voltooid|Bewerkingsresultaten|Geslaagd|Mislukt|Map selecteren|Controlesom mislukt|Actieve bewerking annuleren?"),
        ["pl"] = Extra("Język|Domyślny systemu|Zmiana języka zostanie zastosowana po ponownym uruchomieniu.|Gotowe|Odświeżanie urządzeń…|Nie znaleziono odpowiednich urządzeń wymiennych|Ta platforma nie jest obsługiwana|Przygotowywanie…|Wybierz co najmniej jedno urządzenie.|Operacja zakończona|Wyniki operacji|Sukces|Niepowodzenie|Wybierz folder|Obliczanie sumy kontrolnej nie powiodło się|Anulować aktywną operację?"),
        ["zh"] = Extra("语言|系统默认|语言更改将在重新启动后生效。|就绪|正在刷新设备…|未找到符合条件的可移动设备|不支持此平台|正在准备…|请至少选择一个设备。|操作完成|操作结果|成功|失败|选择文件夹|校验和计算失败|取消当前操作？"),
        ["ja"] = Extra("言語|システムの既定|言語の変更は再起動後に適用されます。|準備完了|デバイスを更新中…|対象のリムーバブルデバイスが見つかりません|このプラットフォームはサポートされていません|準備中…|少なくとも1台のデバイスを選択してください。|操作完了|操作結果|成功|失敗|フォルダーを選択|チェックサムに失敗しました|実行中の操作をキャンセルしますか？"),
    };

    private static string Language = DetectLanguage();

    static Localizer()
    {
        foreach (var (language, values) in Catalogs)
        {
            if (values.Length != Keys.Length)
            {
                throw new InvalidOperationException($"Localization catalog '{language}' has {values.Length} values; expected {Keys.Length}.");
            }
        }
        foreach (var (language, values) in AdditionalCatalogs)
        {
            if (values.Length != AdditionalKeys.Length)
            {
                throw new InvalidOperationException($"Additional localization catalog '{language}' is incomplete.");
            }
        }
    }

    public static string Get(string key)
    {
        if (!KeyIndexes.TryGetValue(key, out var index))
        {
            var additionalIndex = Array.IndexOf(AdditionalKeys, key);
            return additionalIndex < 0 ? key : AdditionalCatalogs[Language][additionalIndex];
        }

        return Catalogs.TryGetValue(Language, out var catalog) ? catalog[index] : Catalogs["en"][index];
    }

    public static void Configure(string? language) => Language =
        string.IsNullOrWhiteSpace(language) || language == "system" ? DetectLanguage() :
        Catalogs.ContainsKey(language) ? language : "en";

    private static string DetectLanguage()
    {
        var language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
        return Catalogs.ContainsKey(language) ? language : "en";
    }

    private static string[] Lines(string value) => value
        .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string[] Extra(string value) => value.Split('|');
}

public sealed class TrExtension(string key) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider) => Localizer.Get(key);
}
