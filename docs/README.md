# Frox landing page

Static, dependency-free GitHub Pages site. Preview it locally from the repo root:

```powershell
py -m http.server 4173 --directory docs
```

Then open `http://127.0.0.1:4173`.

To publish free on GitHub Pages, open the repository's **Settings → Pages**,
choose **Deploy from a branch**, select `main` and the `/docs` folder, and save.
GitHub publishes the project site at `https://imtanmoyd.github.io/frox/`.

## Deploy to Vercel

1. Sign in at [Vercel](https://vercel.com/new) and import `imtanmoyd/frox`
   from GitHub (authorize GitHub access if prompted).
2. Set **Project Name** to `froxva` and **Root Directory** to `docs`.
3. Keep the framework preset as **Other** with no build command; the landing
   page is plain HTML/CSS.
4. Deploy. In **Project → Settings → Domains**, confirm or add
   `froxva.vercel.app` if it is available in your account.
5. Push future landing-page changes to the connected branch for automatic
   deployments.

The Chrome Web Store and desktop download links intentionally remain
unpublished until there is an approved store listing or a signed desktop build.

The repository does not currently have a root `LICENSE` file. Publicly visible
source is not automatically licensed for reuse or redistribution; choose and
add a license before describing code reuse rights as open-source licensed.
