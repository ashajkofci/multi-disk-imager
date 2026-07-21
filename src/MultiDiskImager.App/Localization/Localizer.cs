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
        OperationRead
        OperationWrite
        OperationVerify
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
            Read
            Write
            Verify
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
            Lire
            Écrire
            Vérifier
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
            Lesen
            Schreiben
            Prüfen
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
            Leggi
            Scrivi
            Verifica
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
            Leer
            Escribir
            Verificar
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
            Ler
            Gravar
            Verificar
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
            Lezen
            Schrijven
            Verifiëren
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
            Odczyt
            Zapis
            Weryfikacja
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
            读取
            写入
            验证
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
            読み取り
            書き込み
            検証
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

    private static readonly string[] UiKeys =
    [
        "DevicesAvailable", "DeviceRefreshFailed", "OperationCanceledWarning", "CompletedSuccessfully", "PartialCompletion",
        "SelectExistingImage", "SystemDiskProtected", "ReadRequiresOne", "SelectImagePath", "ImageDoesNotExist",
        "ReadOnlySelected", "RemainingLabel", "CompleteStage", "TransferStage", "VersionLabel", "BytesLabel",
        "SectorsLabel", "SerialLabel", "ReplaceTitle", "AlreadyExists", "Replace", "ImageTooLarge", "CropAndWrite", "StopWarning", "SelectAll",
        "FullDiskAccessMessage", "OpenSystemSettings"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> UiCatalogs = new Dictionary<string, string[]>
    {
        ["en"] = Extra("{0} devices available|Device refresh failed|Operation canceled; a target may contain incomplete data|Completed successfully|Completed on some devices|Select an existing image file first.|The system disk cannot be selected.|Reading requires exactly one source device.|Select an image path.|The selected image does not exist.|One or more selected devices are read-only.|remaining|Complete|Transferring|Version|bytes|byte sectors|Serial|Replace image?|already exists and will be replaced.|Replace|Image is too large|Crop and write|Stopping now may leave the target with incomplete or corrupt data.|Select all|macOS requires Full Disk Access to read or write physical disks. Enable bNovate Multi Disk Imager in System Settings → Privacy & Security → Full Disk Access, then quit and reopen the app.|Open System Settings"),
        ["fr"] = Extra("{0} périphériques disponibles|Échec de l’actualisation des périphériques|Opération annulée ; une cible peut contenir des données incomplètes|Terminée avec succès|Terminée sur certains périphériques|Sélectionnez d’abord un fichier image existant.|Le disque système ne peut pas être sélectionné.|La lecture exige exactement un périphérique source.|Sélectionnez un chemin d’image.|L’image sélectionnée n’existe pas.|Un ou plusieurs périphériques sont en lecture seule.|restant|Terminé|Transfert|Version|octets|secteurs par octet|Numéro de série|Remplacer l’image ?|existe déjà et sera remplacé.|Remplacer|L’image est trop grande|Rogner et écrire|L’arrêt peut laisser la cible avec des données incomplètes ou corrompues.|Tout sélectionner|macOS exige un accès complet au disque pour lire ou écrire sur des disques physiques. Activez bNovate Multi Disk Imager dans Réglages Système → Confidentialité et sécurité → Accès complet au disque, puis quittez et rouvrez l’application.|Ouvrir Réglages Système"),
        ["de"] = Extra("{0} Datenträger verfügbar|Aktualisierung der Datenträger fehlgeschlagen|Vorgang abgebrochen; ein Ziel kann unvollständige Daten enthalten|Erfolgreich abgeschlossen|Auf einigen Datenträgern abgeschlossen|Zuerst eine vorhandene Abbilddatei auswählen.|Der Systemdatenträger kann nicht ausgewählt werden.|Zum Lesen ist genau ein Quelldatenträger erforderlich.|Einen Abbildpfad auswählen.|Das ausgewählte Abbild ist nicht vorhanden.|Mindestens ein ausgewählter Datenträger ist schreibgeschützt.|verbleibend|Abgeschlossen|Übertragung|Version|Bytes|Byte-Sektoren|Seriennummer|Abbild ersetzen?|ist bereits vorhanden und wird ersetzt.|Ersetzen|Das Abbild ist zu groß|Zuschneiden und schreiben|Beim Stoppen können unvollständige oder beschädigte Daten auf dem Ziel verbleiben.|Alle auswählen|macOS benötigt Festplattenvollzugriff, um physische Datenträger zu lesen oder zu beschreiben. Aktivieren Sie bNovate Multi Disk Imager unter Systemeinstellungen → Datenschutz & Sicherheit → Festplattenvollzugriff und starten Sie die App neu.|Systemeinstellungen öffnen"),
        ["it"] = Extra("{0} dispositivi disponibili|Aggiornamento dispositivi non riuscito|Operazione annullata; una destinazione potrebbe contenere dati incompleti|Completata con successo|Completata su alcuni dispositivi|Selezionare prima un file immagine esistente.|Il disco di sistema non può essere selezionato.|La lettura richiede esattamente un dispositivo sorgente.|Selezionare un percorso immagine.|L’immagine selezionata non esiste.|Uno o più dispositivi selezionati sono di sola lettura.|rimanente|Completato|Trasferimento|Versione|byte|settori da byte|Numero di serie|Sostituire l’immagine?|esiste già e verrà sostituito.|Sostituisci|L’immagine è troppo grande|Ritaglia e scrivi|L’arresto potrebbe lasciare dati incompleti o danneggiati sulla destinazione.|Seleziona tutto|macOS richiede l’accesso completo al disco per leggere o scrivere dischi fisici. Abilita bNovate Multi Disk Imager in Impostazioni di Sistema → Privacy e sicurezza → Accesso completo al disco, quindi chiudi e riapri l’app.|Apri Impostazioni di Sistema"),
        ["es"] = Extra("{0} dispositivos disponibles|Error al actualizar los dispositivos|Operación cancelada; un destino puede contener datos incompletos|Completada correctamente|Completada en algunos dispositivos|Seleccione primero un archivo de imagen existente.|No se puede seleccionar el disco del sistema.|La lectura requiere exactamente un dispositivo de origen.|Seleccione una ruta de imagen.|La imagen seleccionada no existe.|Uno o más dispositivos seleccionados son de solo lectura.|restante|Completado|Transfiriendo|Versión|bytes|sectores de bytes|Número de serie|¿Reemplazar imagen?|ya existe y será reemplazado.|Reemplazar|La imagen es demasiado grande|Recortar y escribir|Detener ahora puede dejar datos incompletos o dañados en el destino.|Seleccionar todo|macOS requiere acceso total al disco para leer o escribir discos físicos. Active bNovate Multi Disk Imager en Ajustes del Sistema → Privacidad y seguridad → Acceso total al disco; después, cierre y vuelva a abrir la aplicación.|Abrir Ajustes del Sistema"),
        ["pt"] = Extra("{0} dispositivos disponíveis|Falha ao atualizar dispositivos|Operação cancelada; um destino pode conter dados incompletos|Concluída com sucesso|Concluída em alguns dispositivos|Selecione primeiro um ficheiro de imagem existente.|O disco do sistema não pode ser selecionado.|A leitura requer exatamente um dispositivo de origem.|Selecione um caminho de imagem.|A imagem selecionada não existe.|Um ou mais dispositivos selecionados são só de leitura.|restante|Concluído|A transferir|Versão|bytes|setores de bytes|Número de série|Substituir imagem?|já existe e será substituído.|Substituir|A imagem é demasiado grande|Recortar e gravar|Parar agora pode deixar dados incompletos ou corrompidos no destino.|Selecionar tudo|O macOS requer acesso total ao disco para ler ou gravar discos físicos. Ative o bNovate Multi Disk Imager em Definições do Sistema → Privacidade e Segurança → Acesso total ao disco e, em seguida, feche e reabra a aplicação.|Abrir Definições do Sistema"),
        ["nl"] = Extra("{0} apparaten beschikbaar|Vernieuwen van apparaten mislukt|Bewerking geannuleerd; een doel kan onvolledige gegevens bevatten|Succesvol voltooid|Op sommige apparaten voltooid|Selecteer eerst een bestaand beeldbestand.|De systeemschijf kan niet worden geselecteerd.|Voor lezen is precies één bronapparaat vereist.|Selecteer een beeldpad.|Het geselecteerde beeld bestaat niet.|Een of meer geselecteerde apparaten zijn alleen-lezen.|resterend|Voltooid|Overdragen|Versie|bytes|byte-sectoren|Serienummer|Beeld vervangen?|bestaat al en wordt vervangen.|Vervangen|Het beeld is te groot|Bijsnijden en schrijven|Stoppen kan onvolledige of beschadigde gegevens op het doel achterlaten.|Alles selecteren|macOS vereist volledige schijftoegang om fysieke schijven te lezen of te schrijven. Schakel bNovate Multi Disk Imager in via Systeeminstellingen → Privacy en beveiliging → Volledige schijftoegang en sluit en heropen daarna de app.|Open Systeeminstellingen"),
        ["pl"] = Extra("Dostępne urządzenia: {0}|Odświeżanie urządzeń nie powiodło się|Operacja anulowana; cel może zawierać niepełne dane|Zakończono pomyślnie|Zakończono na części urządzeń|Najpierw wybierz istniejący plik obrazu.|Nie można wybrać dysku systemowego.|Odczyt wymaga dokładnie jednego urządzenia źródłowego.|Wybierz ścieżkę obrazu.|Wybrany obraz nie istnieje.|Co najmniej jedno urządzenie jest tylko do odczytu.|pozostało|Zakończono|Przesyłanie|Wersja|bajtów|sektory bajtowe|Numer seryjny|Zastąpić obraz?|już istnieje i zostanie zastąpiony.|Zastąp|Obraz jest zbyt duży|Przytnij i zapisz|Zatrzymanie może pozostawić na celu niepełne lub uszkodzone dane.|Zaznacz wszystko|System macOS wymaga pełnego dostępu do dysku, aby odczytywać lub zapisywać dyski fizyczne. Włącz bNovate Multi Disk Imager w Ustawienia systemowe → Prywatność i ochrona → Pełny dostęp do dysku, a następnie zamknij i ponownie otwórz aplikację.|Otwórz Ustawienia systemowe"),
        ["zh"] = Extra("{0} 个设备可用|设备刷新失败|操作已取消；目标可能包含不完整数据|已成功完成|已在部分设备上完成|请先选择现有映像文件。|不能选择系统磁盘。|读取操作只允许一个源设备。|请选择映像路径。|所选映像不存在。|一个或多个设备为只读。|剩余|完成|正在传输|版本|字节|字节扇区|序列号|替换映像？|已存在并将被替换。|替换|映像太大|裁剪并写入|立即停止可能使目标包含不完整或损坏的数据。|全选|macOS 需要“完全磁盘访问权限”才能读取或写入物理磁盘。请在“系统设置”→“隐私与安全性”→“完全磁盘访问权限”中启用 bNovate Multi Disk Imager，然后退出并重新打开应用。|打开系统设置"),
        ["ja"] = Extra("{0}台のデバイスが利用可能|デバイスの更新に失敗しました|操作をキャンセルしました。ターゲットに不完全なデータが残る可能性があります|正常に完了しました|一部のデバイスで完了しました|既存のイメージファイルを選択してください。|システムディスクは選択できません。|読み取り元は1台だけ選択してください。|イメージのパスを選択してください。|選択したイメージは存在しません。|選択したデバイスの一部が読み取り専用です。|残り|完了|転送中|バージョン|バイト|バイトセクター|シリアル番号|イメージを置き換えますか？|は既に存在し、置き換えられます。|置き換え|イメージが大きすぎます|切り詰めて書き込む|停止するとターゲットに不完全または破損したデータが残る可能性があります。|すべて選択|物理ディスクを読み書きするには、macOSの「フルディスクアクセス」が必要です。「システム設定」→「プライバシーとセキュリティ」→「フルディスクアクセス」でbNovate Multi Disk Imagerを有効にし、アプリを終了して再度開いてください。|システム設定を開く"),
    };

    private static readonly string[] HelpKeys =
    [
        "Help", "HelpTitle", "HelpIntro", "GettingStarted", "HelpStep1", "HelpStep2", "HelpStep3", "Operations",
        "HelpRead", "HelpWrite", "HelpVerify", "HelpWipe", "HelpOptionsTitle", "HelpOnlyAllocated",
        "HelpVerifyAfter", "HelpChecksum", "HelpSafetyTitle", "HelpSafety", "HelpAdmin", "HelpCancel"
    ];

    private static readonly IReadOnlyDictionary<string, string[]> HelpCatalogs = new Dictionary<string, string[]>
    {
        ["en"] = Extra("Help|Help and quick start|Choose what you want to do, confirm the physical devices, and follow the progress without needing disk-imaging experience.|Getting started|1. Refresh the device list, then identify each physical drive by its model, capacity, and device ID. Use Info when you need more details.|2. Select the physical drive or drives. For writing or verification, also choose the raw image file at the top of the main window.|3. Choose an operation at the bottom of the window. Review every confirmation carefully before allowing access.|Operations|Creates a raw .img file from exactly one selected physical drive. If no output path is set, the app asks where to save it.|Copies the selected raw image to all selected drives in parallel. Existing data and partitions on those drives are overwritten.|Compares the image with the selected drives without changing them. A failed result includes the position of the first different byte.|Removes partition and filesystem metadata near the start and end of each selected drive. This is destructive, but it is not a secure full-disk erase.|Options and checks|Only allocated limits reading or verification to the end of the last detected MBR or GPT partition, which can save time and image space.|Verify after automatically compares the completed read or write with the physical drive before reporting success.|Calculate creates an MD5, SHA-1, or SHA-256 checksum for the selected image file. You can compare it with a trusted checksum to detect changes.|Before you continue|Writing and quick wiping destroy data. Confirm the model, capacity, and device ID of every selected drive. The system disk is hidden for protection.|The operating system may ask for administrator approval only when raw access to a physical drive begins.|Canceling, disconnecting a drive, or losing power during a write can leave that drive incomplete or unusable until it is written again."),
        ["fr"] = Extra("Aide|Aide et démarrage rapide|Choisissez l’opération à effectuer, vérifiez les périphériques physiques et suivez la progression sans avoir besoin d’expérience en imagerie disque.|Premiers pas|1. Actualisez la liste des périphériques, puis identifiez chaque disque physique par son modèle, sa capacité et son identifiant. Utilisez Infos pour plus de détails.|2. Sélectionnez le ou les disques physiques. Pour l’écriture ou la vérification, choisissez également le fichier image brute en haut de la fenêtre principale.|3. Choisissez une opération au bas de la fenêtre. Vérifiez attentivement chaque confirmation avant d’autoriser l’accès.|Opérations|Crée un fichier .img brut à partir d’un seul disque physique sélectionné. Si aucun chemin de sortie n’est défini, l’application demande où l’enregistrer.|Copie l’image brute sélectionnée sur tous les disques choisis en parallèle. Les données et partitions existantes sont écrasées.|Compare l’image aux disques sélectionnés sans les modifier. En cas d’échec, le résultat indique la position du premier octet différent.|Supprime les métadonnées de partition et de système de fichiers au début et à la fin de chaque disque sélectionné. Cette opération est destructive, mais ne constitue pas un effacement sécurisé du disque entier.|Options et contrôles|Lire jusqu’à la dernière partition allouée limite la lecture ou la vérification à la fin de la dernière partition MBR ou GPT détectée, ce qui peut gagner du temps et de l’espace.|Vérifier les données à la fin compare automatiquement la lecture ou l’écriture terminée au disque physique avant d’indiquer la réussite.|Calculer crée une somme de contrôle MD5, SHA-1 ou SHA-256 pour l’image sélectionnée. Comparez-la à une somme fiable pour détecter les modifications.|Avant de continuer|L’écriture et l’effacement rapide détruisent les données. Vérifiez le modèle, la capacité et l’identifiant de chaque disque sélectionné. Le disque système est masqué par sécurité.|Le système d’exploitation peut demander une autorisation d’administrateur uniquement lors du premier accès brut à un disque physique.|L’annulation, la déconnexion d’un disque ou une coupure de courant pendant l’écriture peut rendre ce disque incomplet ou inutilisable jusqu’à une nouvelle écriture."),
        ["de"] = Extra("Hilfe|Hilfe und Schnellstart|Wählen Sie die gewünschte Aktion, prüfen Sie die physischen Datenträger und verfolgen Sie den Fortschritt ohne Vorkenntnisse zur Datenträgerabbilderstellung.|Erste Schritte|1. Aktualisieren Sie die Datenträgerliste und identifizieren Sie jeden physischen Datenträger anhand von Modell, Kapazität und Geräte-ID. Weitere Angaben finden Sie unter Info.|2. Wählen Sie den oder die physischen Datenträger aus. Zum Schreiben oder Überprüfen wählen Sie zusätzlich oben im Hauptfenster die Rohabbilddatei aus.|3. Wählen Sie unten im Fenster eine Aktion. Prüfen Sie jede Bestätigung sorgfältig, bevor Sie den Zugriff erlauben.|Aktionen|Erstellt eine rohe .img-Datei von genau einem ausgewählten physischen Datenträger. Ist kein Ausgabepfad festgelegt, fragt die App nach dem Speicherort.|Kopiert das ausgewählte Rohabbild parallel auf alle ausgewählten Datenträger. Vorhandene Daten und Partitionen werden überschrieben.|Vergleicht das Abbild mit den ausgewählten Datenträgern, ohne sie zu verändern. Bei einem Fehler wird die Position des ersten abweichenden Bytes angegeben.|Entfernt Partitions- und Dateisystemmetadaten am Anfang und Ende jedes ausgewählten Datenträgers. Dies ist destruktiv, aber keine sichere Löschung des gesamten Datenträgers.|Optionen und Prüfungen|Nur bis zur letzten belegten Partition lesen begrenzt das Lesen oder Überprüfen auf das Ende der letzten erkannten MBR- oder GPT-Partition und kann so Zeit und Speicherplatz sparen.|Daten nach Abschluss überprüfen vergleicht den abgeschlossenen Lese- oder Schreibvorgang automatisch mit dem physischen Datenträger, bevor Erfolg gemeldet wird.|Berechnen erstellt eine MD5-, SHA-1- oder SHA-256-Prüfsumme für die ausgewählte Abbilddatei. Durch Vergleich mit einer vertrauenswürdigen Prüfsumme lassen sich Änderungen erkennen.|Bevor Sie fortfahren|Schreiben und Schnelllöschen zerstören Daten. Prüfen Sie Modell, Kapazität und Geräte-ID jedes ausgewählten Datenträgers. Der Systemdatenträger ist zum Schutz ausgeblendet.|Das Betriebssystem fordert möglicherweise erst beim Beginn des Rohzugriffs auf einen physischen Datenträger eine Administratorfreigabe an.|Abbrechen, Trennen eines Datenträgers oder Stromausfall während des Schreibens kann den Datenträger unvollständig oder unbrauchbar machen, bis er erneut beschrieben wird."),
        ["it"] = Extra("Aiuto|Aiuto e avvio rapido|Scegli l’operazione da eseguire, verifica i dispositivi fisici e segui l’avanzamento senza necessità di esperienza con le immagini disco.|Per iniziare|1. Aggiorna l’elenco dei dispositivi, quindi identifica ogni unità fisica tramite modello, capacità e ID dispositivo. Usa Info per maggiori dettagli.|2. Seleziona una o più unità fisiche. Per la scrittura o la verifica, scegli anche il file immagine raw nella parte superiore della finestra principale.|3. Scegli un’operazione nella parte inferiore della finestra. Controlla attentamente ogni conferma prima di consentire l’accesso.|Operazioni|Crea un file .img raw da una sola unità fisica selezionata. Se non è impostato un percorso di destinazione, l’app chiede dove salvarlo.|Copia in parallelo l’immagine raw selezionata su tutte le unità scelte. I dati e le partizioni esistenti vengono sovrascritti.|Confronta l’immagine con le unità selezionate senza modificarle. In caso di errore, il risultato include la posizione del primo byte diverso.|Rimuove i metadati delle partizioni e del file system all’inizio e alla fine di ogni unità selezionata. L’operazione è distruttiva, ma non è una cancellazione sicura dell’intero disco.|Opzioni e controlli|Leggi fino all’ultima partizione allocata limita la lettura o la verifica alla fine dell’ultima partizione MBR o GPT rilevata, risparmiando tempo e spazio.|Verifica i dati al termine confronta automaticamente la lettura o scrittura completata con l’unità fisica prima di segnalare il successo.|Calcola crea un checksum MD5, SHA-1 o SHA-256 per il file immagine selezionato. Puoi confrontarlo con un checksum attendibile per rilevare modifiche.|Prima di continuare|La scrittura e la cancellazione rapida distruggono i dati. Verifica modello, capacità e ID dispositivo di ogni unità selezionata. Il disco di sistema è nascosto per sicurezza.|Il sistema operativo può richiedere l’autorizzazione dell’amministratore solo quando inizia l’accesso raw a un’unità fisica.|L’annullamento, la disconnessione di un’unità o un’interruzione di corrente durante la scrittura possono lasciare l’unità incompleta o inutilizzabile finché non viene riscritta."),
        ["es"] = Extra("Ayuda|Ayuda e inicio rápido|Elija qué desea hacer, confirme los dispositivos físicos y siga el progreso sin necesitar experiencia con imágenes de disco.|Primeros pasos|1. Actualice la lista de dispositivos e identifique cada unidad física por su modelo, capacidad e ID. Use Información si necesita más detalles.|2. Seleccione una o varias unidades físicas. Para escribir o verificar, elija también el archivo de imagen sin procesar en la parte superior de la ventana principal.|3. Elija una operación en la parte inferior de la ventana. Revise cada confirmación con atención antes de permitir el acceso.|Operaciones|Crea un archivo .img sin procesar desde una única unidad física seleccionada. Si no se define una ruta de salida, la aplicación pregunta dónde guardarlo.|Copia en paralelo la imagen sin procesar seleccionada en todas las unidades elegidas. Se sobrescriben los datos y particiones existentes.|Compara la imagen con las unidades seleccionadas sin modificarlas. Si falla, el resultado incluye la posición del primer byte diferente.|Elimina los metadatos de particiones y sistemas de archivos al principio y al final de cada unidad seleccionada. Esta acción es destructiva, pero no es un borrado seguro del disco completo.|Opciones y comprobaciones|Leer hasta la última partición asignada limita la lectura o verificación al final de la última partición MBR o GPT detectada, lo que puede ahorrar tiempo y espacio.|Verificar los datos al finalizar compara automáticamente la lectura o escritura terminada con la unidad física antes de indicar que fue correcta.|Calcular crea una suma MD5, SHA-1 o SHA-256 del archivo de imagen seleccionado. Puede compararla con una suma de confianza para detectar cambios.|Antes de continuar|La escritura y el borrado rápido destruyen datos. Confirme el modelo, la capacidad y el ID de cada unidad seleccionada. El disco del sistema se oculta para protegerlo.|El sistema operativo puede solicitar autorización de administrador solo cuando comienza el acceso sin procesar a una unidad física.|Cancelar, desconectar una unidad o perder la alimentación durante una escritura puede dejar la unidad incompleta o inutilizable hasta que vuelva a escribirse."),
        ["pt"] = Extra("Ajuda|Ajuda e início rápido|Escolha o que pretende fazer, confirme os dispositivos físicos e acompanhe o progresso sem precisar de experiência em imagens de disco.|Primeiros passos|1. Atualize a lista de dispositivos e identifique cada unidade física pelo modelo, capacidade e ID. Use Informação para obter mais detalhes.|2. Selecione uma ou mais unidades físicas. Para gravar ou verificar, escolha também o ficheiro de imagem bruta no topo da janela principal.|3. Escolha uma operação na parte inferior da janela. Reveja atentamente cada confirmação antes de permitir o acesso.|Operações|Cria um ficheiro .img bruto a partir de uma única unidade física selecionada. Se não houver um caminho de saída definido, a aplicação pergunta onde o guardar.|Copia em paralelo a imagem bruta selecionada para todas as unidades escolhidas. Os dados e partições existentes são substituídos.|Compara a imagem com as unidades selecionadas sem as alterar. Em caso de falha, o resultado inclui a posição do primeiro byte diferente.|Remove metadados de partições e sistemas de ficheiros no início e no fim de cada unidade selecionada. Esta operação é destrutiva, mas não constitui uma eliminação segura do disco inteiro.|Opções e verificações|Ler até à última partição alocada limita a leitura ou verificação ao fim da última partição MBR ou GPT detetada, o que pode poupar tempo e espaço.|Verificar os dados no final compara automaticamente a leitura ou gravação concluída com a unidade física antes de indicar sucesso.|Calcular cria uma soma de verificação MD5, SHA-1 ou SHA-256 para o ficheiro de imagem selecionado. Compare-a com uma soma fidedigna para detetar alterações.|Antes de continuar|A gravação e a limpeza rápida destroem dados. Confirme o modelo, a capacidade e o ID de cada unidade selecionada. O disco do sistema fica oculto por segurança.|O sistema operativo pode pedir autorização de administrador apenas quando começa o acesso bruto a uma unidade física.|Cancelar, desligar uma unidade ou perder energia durante uma gravação pode deixar a unidade incompleta ou inutilizável até ser novamente gravada."),
        ["nl"] = Extra("Help|Help en snel aan de slag|Kies wat u wilt doen, controleer de fysieke apparaten en volg de voortgang zonder ervaring met schijfkopieën.|Aan de slag|1. Vernieuw de apparatenlijst en identificeer elke fysieke schijf aan de hand van model, capaciteit en apparaat-ID. Gebruik Info voor meer details.|2. Selecteer een of meer fysieke schijven. Kies voor schrijven of verifiëren ook het onbewerkte beeldbestand bovenaan het hoofdvenster.|3. Kies onderaan het venster een bewerking. Controleer elke bevestiging zorgvuldig voordat u toegang toestaat.|Bewerkingen|Maakt een onbewerkt .img-bestand van precies één geselecteerde fysieke schijf. Als geen uitvoerpad is ingesteld, vraagt de app waar het bestand moet worden opgeslagen.|Kopieert het geselecteerde onbewerkte beeld parallel naar alle geselecteerde schijven. Bestaande gegevens en partities worden overschreven.|Vergelijkt het beeld met de geselecteerde schijven zonder ze te wijzigen. Bij een fout vermeldt het resultaat de positie van de eerste afwijkende byte.|Verwijdert partitie- en bestandssysteemmetadata aan het begin en einde van elke geselecteerde schijf. Dit is destructief, maar wist niet de volledige schijf veilig.|Opties en controles|Lezen tot en met de laatste toegewezen partitie beperkt lezen of verifiëren tot het einde van de laatst gedetecteerde MBR- of GPT-partitie en kan tijd en ruimte besparen.|Gegevens na afloop verifiëren vergelijkt de voltooide lees- of schrijfbewerking automatisch met de fysieke schijf voordat succes wordt gemeld.|Berekenen maakt een MD5-, SHA-1- of SHA-256-controlesom voor het geselecteerde beeldbestand. Vergelijk deze met een vertrouwde controlesom om wijzigingen te detecteren.|Voordat u doorgaat|Schrijven en snel wissen vernietigen gegevens. Controleer model, capaciteit en apparaat-ID van elke geselecteerde schijf. De systeemschijf is ter bescherming verborgen.|Het besturingssysteem kan pas om beheerderstoestemming vragen wanneer rechtstreekse toegang tot een fysieke schijf begint.|Annuleren, een schijf loskoppelen of stroomverlies tijdens het schrijven kan de schijf onvolledig of onbruikbaar maken totdat deze opnieuw wordt beschreven."),
        ["pl"] = Extra("Pomoc|Pomoc i szybki start|Wybierz operację, sprawdź urządzenia fizyczne i śledź postęp bez konieczności znajomości obrazowania dysków.|Pierwsze kroki|1. Odśwież listę urządzeń, a następnie rozpoznaj każdy dysk fizyczny według modelu, pojemności i identyfikatora. Użyj Informacji, aby zobaczyć więcej szczegółów.|2. Wybierz co najmniej jeden dysk fizyczny. Do zapisu lub weryfikacji wybierz również plik surowego obrazu u góry głównego okna.|3. Wybierz operację u dołu okna. Przed zezwoleniem na dostęp dokładnie sprawdź każde potwierdzenie.|Operacje|Tworzy surowy plik .img z dokładnie jednego wybranego dysku fizycznego. Jeśli nie ustawiono ścieżki wyjściowej, aplikacja zapyta, gdzie go zapisać.|Kopiuje równolegle wybrany surowy obraz na wszystkie wybrane dyski. Istniejące dane i partycje zostaną nadpisane.|Porównuje obraz z wybranymi dyskami bez ich zmieniania. Wynik nieudanej weryfikacji zawiera pozycję pierwszego różniącego się bajtu.|Usuwa metadane partycji i systemu plików z początku i końca każdego wybranego dysku. Jest to operacja destrukcyjna, ale nie stanowi bezpiecznego wymazania całego dysku.|Opcje i kontrole|Czytaj do ostatniej przydzielonej partycji ogranicza odczyt lub weryfikację do końca ostatniej wykrytej partycji MBR lub GPT, co może oszczędzić czas i miejsce.|Zweryfikuj dane po zakończeniu automatycznie porównuje ukończony odczyt lub zapis z dyskiem fizycznym przed zgłoszeniem sukcesu.|Oblicz tworzy sumę kontrolną MD5, SHA-1 lub SHA-256 wybranego pliku obrazu. Możesz porównać ją z zaufaną sumą, aby wykryć zmiany.|Przed kontynuowaniem|Zapis i szybkie czyszczenie niszczą dane. Sprawdź model, pojemność i identyfikator każdego wybranego dysku. Dysk systemowy jest ukryty dla bezpieczeństwa.|System operacyjny może poprosić o zgodę administratora dopiero po rozpoczęciu bezpośredniego dostępu do dysku fizycznego.|Anulowanie, odłączenie dysku lub utrata zasilania podczas zapisu może pozostawić dysk niekompletny lub bezużyteczny do czasu ponownego zapisania."),
        ["zh"] = Extra("帮助|帮助和快速入门|选择要执行的操作，确认物理设备并跟踪进度，无需具备磁盘映像经验。|开始使用|1. 刷新设备列表，然后根据型号、容量和设备 ID 识别每个物理磁盘。需要更多详细信息时，请使用“信息”。|2. 选择一个或多个物理磁盘。执行写入或验证时，还要在主窗口顶部选择原始映像文件。|3. 在窗口底部选择一项操作。允许访问前，请仔细检查每个确认提示。|操作|从选中的唯一物理磁盘创建原始 .img 文件。如果未设置输出路径，应用会询问保存位置。|将选中的原始映像并行复制到所有选定磁盘。磁盘上的现有数据和分区将被覆盖。|将映像与选定磁盘进行比较，但不作修改。验证失败时，结果会包含第一个不同字节的位置。|删除每个选定磁盘开头和末尾附近的分区及文件系统元数据。此操作会破坏数据，但并非安全擦除整个磁盘。|选项和检查|“读取到最后一个已分配分区”会将读取或验证限制到最后一个已检测到的 MBR 或 GPT 分区末尾，从而节省时间和映像空间。|“完成后验证数据”会在报告成功前，自动将完成的读取或写入结果与物理磁盘进行比较。|“计算”会为选定的映像文件创建 MD5、SHA-1 或 SHA-256 校验和。您可以将其与可信校验和比较以检测更改。|继续之前|写入和快速擦除会破坏数据。请确认每个选定磁盘的型号、容量和设备 ID。为保护系统磁盘，它不会显示。|只有在开始直接访问物理磁盘时，操作系统才可能请求管理员授权。|写入期间取消操作、断开磁盘或断电，可能导致该磁盘数据不完整或无法使用，直至重新写入。"),
        ["ja"] = Extra("ヘルプ|ヘルプとクイックスタート|実行する操作を選び、物理デバイスを確認して進行状況を追跡できます。ディスクイメージ作成の経験は必要ありません。|はじめに|1. デバイス一覧を更新し、モデル、容量、デバイスIDで各物理ドライブを識別します。詳細が必要な場合は「情報」を使用してください。|2. 物理ドライブを選択します。書き込みまたは検証では、メインウィンドウ上部でRAWイメージファイルも選択します。|3. ウィンドウ下部で操作を選択します。アクセスを許可する前に、各確認内容を注意深く確認してください。|操作|選択した1台の物理ドライブからRAW .imgファイルを作成します。出力先が設定されていない場合、保存場所を尋ねます。|選択したRAWイメージを、選択したすべてのドライブへ並列にコピーします。既存のデータとパーティションは上書きされます。|選択したドライブを変更せずにイメージと比較します。失敗した場合、最初に異なるバイトの位置が結果に表示されます。|選択した各ドライブの先頭と末尾付近にあるパーティションおよびファイルシステムのメタデータを削除します。これは破壊的な操作ですが、ディスク全体の安全な消去ではありません。|オプションと確認|「最後に割り当てられたパーティションまで読み取る」は、読み取りまたは検証を最後に検出されたMBRまたはGPTパーティションの末尾までに制限し、時間と容量を節約します。|「完了後にデータを検証」は、成功を報告する前に、完了した読み取りまたは書き込みを物理ドライブと自動的に比較します。|「計算」は選択したイメージファイルのMD5、SHA-1、またはSHA-256チェックサムを作成します。信頼できるチェックサムと比較して変更を検出できます。|続行する前に|書き込みとクイック消去はデータを破壊します。選択した各ドライブのモデル、容量、デバイスIDを確認してください。システムディスクは保護のため非表示です。|物理ドライブへのRAWアクセスを開始するときにのみ、OSが管理者の承認を求める場合があります。|書き込み中のキャンセル、ドライブの切断、または停電により、再度書き込むまでドライブが不完全または使用不能になる場合があります。"),
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
        foreach (var (language, values) in UiCatalogs)
        {
            if (values.Length != UiKeys.Length) throw new InvalidOperationException($"UI localization catalog '{language}' is incomplete.");
        }
        foreach (var (language, values) in HelpCatalogs)
        {
            if (values.Length != HelpKeys.Length) throw new InvalidOperationException($"Help localization catalog '{language}' is incomplete.");
        }
    }

    public static string Get(string key)
    {
        if (!KeyIndexes.TryGetValue(key, out var index))
        {
            var additionalIndex = Array.IndexOf(AdditionalKeys, key);
            if (additionalIndex >= 0) return AdditionalCatalogs[Language][additionalIndex];
            var uiIndex = Array.IndexOf(UiKeys, key);
            if (uiIndex >= 0) return UiCatalogs[Language][uiIndex];
            var helpIndex = Array.IndexOf(HelpKeys, key);
            return helpIndex < 0 ? key : HelpCatalogs[Language][helpIndex];
        }

        return Catalogs.TryGetValue(Language, out var catalog) ? catalog[index] : Catalogs["en"][index];
    }

    public static string Format(string key, params object?[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

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
