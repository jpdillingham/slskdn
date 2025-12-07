# Multi-Source Downloads - Experimental Feature

## Overview

Multi-source downloading allows slskdn to download a single file from multiple peers simultaneously, similar to BitTorrent's "swarm" behavior. This can significantly improve download speeds for popular files.

## How It Works

### Swarm Mode (Chunked Downloads)

1. **Search & Pool Building**: Find all peers sharing an identical file (matched by exact file size)
2. **Chunk Division**: Split the file into small chunks (default: 128KB)
3. **Worker Distribution**: Spawn a worker for each available source
4. **Shared Queue**: All workers grab chunks from a shared `ConcurrentQueue`
5. **Fast Peers Do More**: Workers that finish quickly grab another chunk immediately
6. **Retry Logic**: Failed chunks are re-queued for other workers
7. **Proven Source Retry**: After initial pass, retry remaining chunks using only sources that succeeded
8. **Assembly**: Combine all chunks into final file

### Key Findings

#### What Works

- **Partial downloads via `startOffset`**: Soulseek protocol supports starting downloads at arbitrary byte offsets
- **`LimitedWriteStream` workaround**: Since Soulseek requires full file size even for partial downloads, we wrap the output stream to cancel after receiving the desired chunk
- **Concurrent chunk downloads**: Multiple workers can grab different chunks simultaneously
- **Proven source retry**: After first pass, re-using only successful sources dramatically improves completion rate

#### Limitations Discovered

1. **Many clients reject partial downloads**: Most Soulseek clients will reject download requests with `startOffset > 0`, reporting "Download failed by remote client"
2. **Single download per user per file**: Soulseek only allows one active download of a specific file from a specific user at a time
3. **Variable client behavior**: Some clients work perfectly with partial downloads, others reject immediately

#### Speed Thresholds

- **Minimum speed**: 5 KB/s
- **Slow duration**: 15 seconds
- Workers downloading slower than 5 KB/s for 15+ consecutive seconds are cycled out
- Their chunk is re-queued for faster peers

#### Retry Behavior

- Workers tolerate up to 3 consecutive failures before giving up
- Failed chunks are always re-queued for other workers
- After initial pass completes, up to 3 retry rounds using only "proven" sources (those that succeeded at least once)

## API Endpoints

### POST `/api/v0/multisource/download`

Direct download with pre-verified sources:

```json
{
  "filename": "path/to/file.flac",
  "fileSize": 21721524,
  "chunkSize": 131072,
  "sources": [
    {"username": "user1", "fullPath": "their/path/file.flac"},
    {"username": "user2", "fullPath": "different/path/file.flac"}
  ]
}
```

### POST `/api/v0/multisource/swarm`

Search and download in one call:

```json
{
  "filename": "search term",
  "size": 21721524,
  "chunkSize": 131072,
  "searchTimeout": 30000
}
```

## Test Scripts

- `swarm-test.sh` - Main test script with pool management
  - `./swarm-test.sh refresh "search term" 30` - Build fresh pool
  - `./swarm-test.sh targets` - Show available files
  - `./swarm-test.sh swarm SIZE CHUNK_KB` - Run swarm download
  - `./swarm-test.sh auto` - Full auto test

- `build-pool.sh` - Build and manage source pools
- `race-download.sh` - Alternative "race mode" (all sources download full file, first wins)

## Configuration

Default chunk size: 128KB (configurable per request)

Recommended settings:
- Chunk size: 64-256KB (smaller = more parallelism, larger = less overhead)
- Minimum sources: 3+ for meaningful benefit
- Speed threshold: 5 KB/s minimum, 15s tolerance

## Known Issues / TODO

1. **Slow peer replacement**: Currently slow peers are cycled out but idle proven peers don't automatically take over their work
2. **No dynamic source discovery**: Pool is built once at start; new sources aren't discovered mid-download
3. **Chunk verification**: Currently relies on size matching; could add hash verification for integrity
4. **Progress reporting**: Live progress could be improved for frontend integration

## Sample Results

Typical successful swarm download:
```
[SWARM] ✓ user1 chunk 0 @ 2847 KB/s [1/197]
[SWARM] ✓ user2 chunk 1 @ 1523 KB/s [2/197]
[SWARM] ✓ user1 chunk 5 @ 3012 KB/s [3/197]
...
[SWARM] SUCCESS! Chunk distribution:
  user1: 87 chunks
  user2: 54 chunks
  user3: 32 chunks
  user4: 24 chunks
```

Fast peers naturally complete more chunks due to the shared queue design.

