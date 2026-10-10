# DuelBot Studio

Aplicación WPF para consultar cartas, editar mazos y configurar reglas de un bot.

## Organizacion

- `Models/`: modelos de cartas, mazos y reglas del bot.
- `Services/`: lectura de archivos `.ydk`, gestión del mazo y acceso a datos e imágenes de cartas.
- `Views/MainWindow.xaml`: composición de la ventana principal y coordinación entre funcionalidades.
- `Views/Controls/CardDetailsView`: presentación de la carta seleccionada.
- `Views/Controls/DeckViewerView`: visualización y eliminación de cartas del mazo.
- `Views/Controls/DeckEditorView`: búsqueda y adición de cartas.
- `Views/Controls/BotEditorView`: punto de extension aislado para el futuro editor de reglas.

Cada funcionalidad de pantalla vive en su propio `UserControl`; la ventana principal solo conecta los controles y gestiona la carga del mazo. Mantener esta separación evita que la lógica del editor de bot, el visor y la búsqueda se acumulen en `MainWindow`.
