

# YTLiveChat

Biblioteca .NET no oficial para leer el chat en vivo de YouTube a través de InnerTube (la misma interfaz web que utiliza YouTube), sin cuotas de la API de Datos ni configuración de OAuth.

[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/Agash/YTLiveChat/publish.yml?style=flat-square&logo=github&logoColor=white)](https://github.com/Agash/YTLiveChat/actions)
[![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.DependencyInjection.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.YTLiveChat.DependencyInjection/)
[![NuGet Version](https://img.shields.io/nuget/v/Agash.YTLiveChat.svg?style=flat-square&logo=nuget&logoColor=white)](https://www.nuget.org/packages/Agash.YTLiveChat/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](https://opensource.org/licenses/MIT)

## Frameworks objetivo

- `net10.0`
- `net9.0`
- `netstandard2.1`
- `netstandard2.0`

## Instalación

Paquete principal:

```bash
dotnet add package Agash.YTLiveChat
```

Con ayudantes de DI:

```bash
dotnet add package Agash.YTLiveChat.DependencyInjection
```

## Qué incluye

**Mensajes de chat**
- Mensajes de chat (`ChatReceived`) — texto, emojis, imágenes
- Super Chats / Super Stickers con cantidad + moneda analizadas
- Eventos de membresía — nuevo acceso, hito, compra de regalo, canje de regalo, mejora de nivel (`MembershipDetails.EventType`)
- Soporte para ticker (`addLiveChatTickerItemAction`) — mensajes pagados, elementos de membresía, anuncios de compra de regalos; los elementos del ticker incluyen ID del canal del autor, miniatura del autor y `Author.ChannelHandle` (el `@manejador`) cuando está disponible
- Extracción del rango en el tablero de líderes de espectadores a través de `ChatItem.ViewerLeaderboardRank` (etiquetas de corona de puntos de YouTube como `#1`)

**Moderación y ciclo de vida**
- Mensaje eliminado (`ChatItemDeleted`) — se eliminó un solo elemento
- Autor baneado/limpiado (`ChatItemsDeletedByAuthor`) — se eliminaron todos los mensajes de un canal
- Mensaje reemplazado (`ChatItemReplaced`) — resolución de modo lento o marcador de posición

**Encuestas**
- `PollUpdated` — se activa cuando se abre una nueva encuesta (`Poll.IsNew == true`) y en cada actualización de conteo de votos (`IsNew == false`); incluye `Question` (`MessagePart[]?`), `Choices` estructurados (cada uno con `Text: MessagePart[]` y `VoteRatio`), `CreatorHandle` y `TotalVotes`
- `PollClosed` — se activa cuando se descarta el panel de la encuesta; usa `PollId` para correlacionar con los eventos `PollUpdated` anteriores
- Después de `PollClosed`, se activa `EngagementMessageReceived` con `MessageType == PollResult` llevando el resumen final en `Message` (`MessagePart[]`, no obligatorio para el seguimiento del ciclo de vida de la encuesta)

**Banners**
- `BannerAdded` — se activa con una subclase `BannerItem`; usa coincidencia de patrones para distinguir:
  - `PinnedMessageBannerItem` — mensaje de chat fijado; incluye `Author`, `Message` (`MessagePart[]`), `PinnedBy`, `Timestamp` y banderas de rol (`IsOwner`, `IsModerator`, `IsVerified`)
  - `CrossChannelRedirectBannerItem` — banner de cruce de canales; verifica `RedirectType` (`Redirect` = el propietario redirige a los espectadores a otra transmisión, `Raid` = espectadores de otro canal se unen aquí); incluye `RedirectChannelHandle` (el `@manejador`), `RedirectVideoId` (no nulo solo para `Redirect`) y `BannerMessage` (`MessagePart[]`)
  - `ChatSummaryBannerItem` — resumen de chat generado por IA (característica experimental de YouTube); incluye `Summary` (`MessagePart[]`) con título en negrita, descargo de responsabilidad atenuado y fragmentos de texto del cuerpo, más `SummaryId`
- `BannerRemoved` — banner descartado; `TargetActionId` coincide con el `ActionId` del `BannerAdded` anterior

**Mensajes del sistema / interacción**
- `EngagementMessageReceived` — avisos generados por YouTube en el feed del chat; `Message` es `MessagePart[]`:
  - `CommunityGuidelines` — recordatorio de bienvenida/lineamientos al inicio de la transmisión
  - `SubscribersOnly` — aviso de modo solo para suscriptores
  - `PollResult` — resumen formateado de resultados de la encuesta (ver Encuestas arriba)

**Acceso sin procesar (Raw)**
- `RawActionReceived` — cada acción de InnerTube, incluyendo las no admitidas
- APIs de transmisión asíncrona (`StreamChatItemsAsync`, `StreamRawActionsAsync`)

## Consideraciones importantes

- Este es un analizador no oficial sobre el esquema interno de YouTube. Las cargas de datos (payloads) pueden cambiar en cualquier momento.
- Esta biblioteca solo lee chat (no envía mensajes).
- Respeta la frecuencia de solicitudes para evitar límites de velocidad o bloqueos temporales.

## Aviso sobre API Beta

El modo de monitor continuo de transmisiones en vivo está actualmente en **BETA/NO SOPORTADO** y puede cambiar o dejar de funcionar en cualquier momento:

- `YTLiveChatOptions.EnableContinuousLivestreamMonitor`
- `YTLiveChatOptions.LiveCheckFrequency`
- `IYTLiveChat.LivestreamStarted`
- `IYTLiveChat.LivestreamEnded`
- `IYTLiveChat.LivestreamInaccessible`

Estos miembros emiten intencionalmente advertencias del compilador a través de `[Obsolete]` para señalar el estado inestable de la API.

Nota sobre el monitor: la resolución de la página del canal/visualización se obtiene mediante solicitudes sin estado (sin cookies) dentro de la biblioteca para reducir los bucles de pantallas de consentimiento durante sesiones de monitor de larga duración.

## Inicio rápido (DI)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YTLiveChat.Contracts;
using YTLiveChat.Contracts.Services;
using YTLiveChat.DependencyInjection;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddYTLiveChat(builder.Configuration);

builder.Services.Configure<YTLiveChatOptions>(options =>
{
    options.RequestFrequency = 1000;
    options.DebugLogReceivedJsonItems = true;
    options.DebugLogFilePath = "logs/ytlivechat_debug.json";
});

builder.Services.AddHostedService<ChatWorker>();
await builder.Build().RunAsync();
```

Ejemplo de Worker:

```csharp
using YTLiveChat.Contracts.Models;
using YTLiveChat.Contracts.Services;

public sealed class ChatWorker(IYTLiveChat chat) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        chat.InitialPageLoaded += (_, e) => Console.WriteLine($"Loaded: {e.LiveId}");
        chat.ChatReceived += (_, e) => HandleChat(e.ChatItem);
        chat.RawActionReceived += (_, e) =>
        {
            if (e.ParsedChatItem is null)
            {
                // Unsupported action still available here
                Console.WriteLine("RAW action received.");
            }
        };
        chat.ChatStopped += (_, e) => Console.WriteLine($"Stopped: {e.Reason}");
        chat.ErrorOccurred += (_, e) => Console.WriteLine($"Error: {e.GetException().Message}");

        chat.Start(handle: "@channelHandle");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct)
    {
        chat.Stop();
        return Task.CompletedTask;
    }

    private static void HandleChat(ChatItem item)
    {
        // inspect item.Superchat, item.MembershipDetails, item.ViewerLeaderboardRank, item.IsTicker
        // for ticker events: item.Author.ChannelId, item.Author.Thumbnail, item.Author.ChannelHandle
    }
}
```

## API de transmisión asíncrona

```csharp
await foreach (ChatItem item in chat.StreamChatItemsAsync(handle: "@channel", cancellationToken: ct))
{
    Console.WriteLine($"{item.Author.Name}: {string.Join("", item.Message.Select(ToText))}");
}

await foreach (RawActionReceivedEventArgs raw in chat.StreamRawActionsAsync(liveId: "videoId", cancellationToken: ct))
{
    if (raw.ParsedChatItem is null)
    {
        Console.WriteLine(raw.RawAction.ToString());
    }
}

static string ToText(MessagePart part) => part switch
{
    TextPart t => t.Text,
    EmojiPart e => e.EmojiText ?? e.Alt ?? "",
    _ => ""
};
```

`TextPart` lleva banderas `Bold`, `Italics` e `IsDeemphasized` para que los consumidores puedan renderizar el formato sin volver a analizar. Todos los campos de mensajes estructurados en la biblioteca (`ChatItem.Message`, `EngagementItem.Message`, `PollChoice.Text`, `PollItem.Question`, resumen/banner `Summary`/`BannerMessage`/`Message` de banners) son `MessagePart[]` — concatena los valores de `TextPart.Text` para texto sin formato, o usa coincidencia de patrones en las banderas para un renderizado enriquecido.

```csharp
```

## Captura de JSON sin procesar para análisis de esquema

Habilitar:

```csharp
options.DebugLogReceivedJsonItems = true;
options.DebugLogFilePath = "logs/ytlivechat_debug.json";
```

El archivo se escribe como una matriz JSON válida, por lo que es directamente parseable por herramientas/scripts.

## Aplicación de ejemplo

`YTLiveChat.Example` incluye:

- Configuración de consola UTF-8 para salida multilingüe
- Renderizado TUI de una línea con colores
- Etiquetado de rango/ticker/membresía/superchat
- Pistas de acciones sin procesar no admitidas
- Opción de captura de JSON sin procesar
- Opción de modo de monitor continuo (beta)

## Lagunas actuales en la cobertura del esquema

- Los objetivos del creador aún no están mapeados (esperando suficientes muestras sin procesar estables).

## Contribuciones

Los informes de errores y muestras de carga de datos (payloads) sin procesar son de gran valor.  
Si agregas soporte de analizador para nuevas cargas de datos, incluye:

- actualizaciones del modelo de respuesta en `YTLiveChat/Models/Response/LiveChatResponse.cs`
- actualizaciones del analizador en `YTLiveChat/Helpers/Parser.cs`
- pruebas + accesorios en `YTLiveChat.Tests`

## Agradecimientos

- [**WolfwithSword**](https://github.com/WolfwithSword) — pruebas y comentarios

## Licencia

MIT. Ver `LICENSE.txt`.
