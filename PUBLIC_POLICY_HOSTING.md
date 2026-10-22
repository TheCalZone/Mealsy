# Mealsy public privacy pages

Two static pages are ready for GitHub Pages:

- Privacy: https://thecalzone.github.io/Mealsy/privacy/
- Account deletion: https://thecalzone.github.io/Mealsy/delete-account/

The app configuration uses these URLs. They are **planned URLs until Pages is
activated**; this cloud environment cannot inspect/enable repository Pages settings
or verify the Pages hostname because its network policy blocks api.github.com and
thecalzone.github.io (CONNECT 403). No claim of live publication is made.

## Fastest activation: prepared gh-pages branch

The separate `gh-pages` branch contains ONLY the five static public policy/site
files. No Firebase web config, app source, binaries or credentials are published.
In GitHub open TheCalZone/Mealsy → Settings → Pages:

1. Under Build and deployment choose **Deploy from a branch**.
2. Select **gh-pages**, folder **/(root)**, then Save.
3. Wait for the Pages build/deployment to finish. Open both URLs above in a private
   browser window and verify that neither requires GitHub sign-in. Pages availability
   for private repositories depends on the GitHub plan/settings; do not make the
   entire app repository public merely to host these pages.
4. Run `python3 scripts/check-public-policies.py --live` from a workstation with
   network access. Set these exact URLs in Play Console's privacy and account/data
   deletion fields only after that public-access check.

## Optional automatic publishing through GitHub Actions

`.github/workflows/publish-policies.yml` builds and deploys only the allowlisted
public files when their Android-source branch changes. To use it instead of the
branch mode, set Pages Source to **GitHub Actions**, then re-run the latest
**Publish Mealsy privacy pages** run under Actions (or merge the workflow into the
default branch for manual dispatch). A first run can fail until Pages is enabled;
re-run after setting it up. Pick one publication mode rather than relying on both.

In branch mode, keep the gh-pages snapshot updated when policy files change. Build
fresh assets with `python3 scripts/build-public-policies.py --output /tmp/policies-site`
and publish only those files to the gh-pages branch. Never publish firebase/desktop
wholesale: it also contains the separate desktop app and its Firebase setup.

## Operational requirements remain

The confirmed support address is Callum@brightbark.co.uk. It must be monitored;
email requests need identity verification before deletion. The pages describe an
email route that does not require installation/sign-in and explain that automatic
deletion is available only where its server rollout is complete. No server rollout
or deletion of real user data is performed by this static-site publication.

Public pages no longer contain publisher checklist/draft notices. They describe
current diagnostic defaults, earlier versions, optional profile information,
Firebase processing, household sharing, subscriptions and deletion retention.
Keep them in sync with actual deployed behaviour and your Data safety declaration.
Hosting resolves the URL requirement only after activation; target SDK, 16 KB
support, account-deletion rollout and store declarations remain separate gates.
