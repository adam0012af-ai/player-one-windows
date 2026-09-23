# Player One Android -> Windows parity checklist

Source baseline: Android branch `fix/codemagic-v120-focus-v121`.

## Foundation
- [x] Same API base: https://playeronetv.site
- [x] Windows stable hardware identity and device label
- [x] /api/device/bootstrap
- [x] /api/devices/status
- [x] /api/device/playlists
- [x] /api/device/playlist-config
- [x] Local device session persistence
- [ ] Linked-device/account grouping verification against backend

## Features that must be complete before Setup EXE
- [ ] Playlist center: M3U, Xtream, host alias/code, add/edit/delete/select/sync
- [ ] Home parity
- [ ] Live browser/categories/search/favorites
- [ ] Live player, mini/fullscreen, reconnect, channel overlay
- [ ] Movies browser/categories/search/favorites
- [ ] Series browser/seasons/episodes/search/favorites
- [ ] Continue Watching and playback state
- [ ] Audio tracks/subtitles/aspect controls
- [ ] Background/minimize behavior appropriate for Windows
- [ ] Settings, About/Info, device ID/key, subscription status/end date
- [ ] Arabic/English and RTL/LTR
- [ ] Cache/performance/error handling
- [ ] Keyboard/remote navigation equivalents
- [ ] Update flow
- [ ] Final QA and AppVeyor installer build

No final Setup EXE should be produced until this checklist is completed and verified.
