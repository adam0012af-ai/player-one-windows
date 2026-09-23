# Player One Android -> Windows parity checklist

Source baseline: Android branch `fix/codemagic-v120-focus-v121`.

## Implemented foundation
- [x] Same Player One API base and device bootstrap/status
- [x] Stable Windows hardware identity (`windows_pc`)
- [x] Device session persistence
- [x] Playlist retrieval/configuration
- [x] M3U + Xtream + Host Code/provider login
- [x] Playlist add/delete and API update support
- [x] Live / Movies / Series catalog parsing
- [x] Search and category filtering
- [x] Xtream series seasons/episodes
- [x] Movie metadata/details
- [x] Favorites state
- [x] Continue Watching state and resume seek
- [x] Fullscreen/aspect/seek/pause controls
- [x] Windows compact always-on-top player behavior
- [x] Live TS/HLS alternate-source fallback
- [x] Auto reconnect preference
- [x] Device Info/status/end-date screen
- [x] Arabic/English preference foundation
- [x] Audio/subtitle language preferences
- [x] LibVLC Windows dependencies added for advanced media parity
- [x] Update-check service foundation

## Must still pass before final Setup EXE
- [ ] Verify linked-device/account grouping behavior against production backend
- [ ] Complete edit/select UX in Playlist Center
- [ ] Replace WPF MediaElement playback surface with LibVLC implementation
- [ ] Wire real audio-track selector
- [ ] Wire real subtitle-track selector
- [ ] Auto-next episodes
- [ ] Complete Arabic strings + RTL on every screen
- [ ] Live channel overlay / fast channel switching
- [ ] Final visual polish and keyboard/remote focus QA
- [ ] Compile successfully on Windows CI
- [ ] Runtime smoke test on Windows 10/11
- [ ] Build installer and final Player-One-Windows-Setup.zip

Final Setup EXE is blocked until every item above is verified.
