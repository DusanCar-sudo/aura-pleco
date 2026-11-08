# Aura Pleco — release announcement drafts (2026-11-08)

## Social post (LinkedIn / X)

> Your workstation leaks RAM all day — and if you run local AI models, it's worse:
> aborted Ollama pulls, stale Hugging Face weights, PyTorch compile caches, temp
> junk. All silent.
>
> I built Aura Pleco 🐟 — a ~3 MB background "cleaner fish" for Linux & Windows
> that watches your memory headroom every 20 seconds, scrubs AI model caches
> (never your models), trims working sets without killing anything, and sweeps
> temp litter. Everything it frees is logged.
>
> 1-click install on both platforms (no Visual Studio needed on Windows).
> Apache-2.0, source + landing page:
> https://github.com/DusanCar-sudo/aura-pleco

## Deploy the landing page (2 options)

1. **GitHub Pages (free, in-repo):**
   - `git add site/ && git commit && git push`
   - Repo Settings → Pages → Deploy from branch: `main`, folder `/site` (or `/docs` after rename).
   - Live at `https://dusancar-sudo.github.io/aura-pleco/`
2. **Vercel:** `npx vercel site/ --prod` from a clean copy.

## Remaining nits before announcing

- `aura-treasurer` is dirty + untracked — unrelated project, decide keep/delete separately.
- Consider a short demo GIF of `aura-pleco --stream` for the README top and the landing hero.
