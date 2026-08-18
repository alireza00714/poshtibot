# LM Studio (local LLM)

This service talks to LM Studio over the OpenAI-compatible API on `http://localhost:1234/v1`. Chat and embeddings are **separate models**. Namadno business APIs stay Mock until real URLs exist.

This machine: ~24 GB RAM, Intel Iris Xe (no NVIDIA). CPU-only inference.

## Models

| Role | Model | Size | Notes |
|---|---|---|---|
| Chat | `qwen3.5-9b` | ~5.5 GB (Q4_K_M) | Best Persian quality at this size. ~8 GB RAM at runtime. |
| Embedding | `Qwen3-Embedding-0.6B` | ~1.2 GB (Q8_0) | 1024-dim, strong Persian support via Qwen3 family. |

Total RAM: ~11 GB for both models + OS overhead. Fits comfortably in 24 GB.

### Why Qwen3.5 9B?

- Qwen3.5 is the latest generation with significantly improved multilingual (including Persian) capability
- 9B dense model — better quality than the old qwen2.5-7b-instruct, similar speed on CPU
- Q4_K_M quantization keeps it under 6 GB on disk

### Previous model

`qwen2.5-7b-instruct` — produced grammatically awkward Persian in conversational responses. Replaced.

## Setup

1. LM Studio is already installed (`lms` CLI).
2. Download the chat model in LM Studio **Discover** tab: search `qwen3.5-9b`, pick the Q4_K_M GGUF.
   Or via CLI:

```bash
lms get "qwen3.5-9b" -y --gguf
```

2.5. Download the embedding GGUF (Qwen3-Embedding-0.6B, 1024-dim).
      LM Studio’s staff-picks lookup may not find it, so download via the full HuggingFace URL:

```bash
lms get "https://huggingface.co/majentik/Qwen3-Embedding-0.6B-GGUF-Q4_K_M" -y --gguf
```

3. Start the server and load both models:

```bash
lms server start --port 1234 --bind 0.0.0.0
lms load "qwen3.5-9b" -y
# NOTE: After downloading the embedding GGUF, use the exact embedding
# model "id" shown in LM Studio (or from /v1/models) below.
# Example (model id may differ):
# lms load "Qwen3-Embedding-0.6B-Q4_K_M" -y
```

`--bind 0.0.0.0` lets Docker (`host.docker.internal:1234`) reach the server.

4. Confirm:

```bash
curl http://localhost:1234/v1/models
```

## Runtime config

| Key | Value |
|---|---|
| `LLM__Provider` | `OpenAICompatible` |
| `LLM__BaseUrl` | `http://localhost:1234/v1` (Compose: `http://host.docker.internal:1234/v1`) |
| `LLM__Model` | `qwen3.5-9b` |
| `LLM__EnableThinking` | `false` — disables Qwen3.5 reasoning so replies stay short and direct |
| `Embedding__Provider` | `OpenAICompatible` |
| `Embedding__Model` | Set this to the embedding model `id` returned by `curl http://localhost:1234/v1/models` |
| `Embedding__Dimensions` | `1024` |

If LM Studio is down, chat still works: approved FAQ text, tools, and safe fallback. Embeddings are rebuilt in the background on API start.

## What the model is allowed to do

Exact FAQ matches stay `DIRECT_FAQ` (no paraphrase). Other retrieved FAQs go through LM Studio as **grounded RAG** (`RAG_GENERATED`). The model may only use approved FAQ excerpts. It must not invent fees, SLAs, or investment advice.

Keep LM Studio running while the API is up. CPU inference on 9B is slower than GPU; first token after load can take 15-30 seconds.
