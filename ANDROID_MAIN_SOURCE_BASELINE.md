# Android main source baseline for Windows parity

This branch ports Player One Windows from the Android application in the official Android repository **main** branch only.

- Android repository: adam0012af-ai/tv-platform-
- Android source ref: main
- Pinned Android commit: d01a3fbb718ec151af053befc171ca297114d6af
- Android client files observed at baseline: 69
- Windows parity branch: windows/android-parity-clean

## Rules
1. Android main is the only product/UI/behavior source of truth for this port.
2. Do not import features, screens, styling, flows, or behavior from other Android branches.
3. Do not add Windows-only product features. Platform adaptation is limited to what is technically required for Windows input, windowing, storage, playback, packaging, and OS integration.
4. Preserve Player One backend contracts, activation semantics, playlist behavior, content flows, library state, settings, and player behavior represented by Android main.
5. Validate parity screen-by-screen and flow-by-flow before calling the Windows build complete.
6. If Android main changes intentionally, update this pinned baseline explicitly before porting the new behavior.

## Android main parity inventory
Home: HomeV8.kt, HomeV9.kt
Catalog: CatalogBrowserV6.kt, CinematicCatalogActivity.kt, CinematicCatalogRepository.kt, CinematicCatalogUi.kt
Live: LiveBrowserV5.kt, LivePlayerV4.kt, CinematicLiveActivity.kt, CinematicLiveRepository.kt, CinematicLiveUi.kt, CinematicLivePlayer.kt
Movies/details: MovieDetailsV4.kt, CinematicDetailsUi.kt
Series: PremiumSeriesEpisodes.kt
Player: CatalogPlayerV3.kt, PlayerOneMediaController.kt, PlaybackLifecycle.kt, PlayerOnePip.kt, PlayerOnePlaybackService.kt
Library/resume: CinematicLibraryActivity.kt, CinematicResumeActivity.kt, PlayerStateStore.kt
Playlists/device/account: PlaylistCenterV6.kt, PlaylistManagerActivity.kt, DeviceAccountRepository.kt, CinemaDeviceBootstrap.kt
Settings/info: SettingsCenterV8.kt, SettingsCenterV9.kt, CinematicSettingsActivity.kt, PlayerOneAdvancedSettings.kt, PlayerOnePreferences.kt, PlayerOneInfoUi.kt, PlayerOneInfoUiV7.kt, PlayerOneSubscriptionInfo.kt
Theme/i18n: CinematicTheme.kt, CinematicChrome.kt, PlayerOneI18n.kt
