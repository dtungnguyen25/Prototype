Scriptable Render Pipeline assets in SSC do not suppport URP and HDRP in the same project.
IMPORTANT: Do not import these packages without URP/HDRP installed, else you will get many errors. 

1. Import the entire asset (e.g. Sci-Fi Ship Controller) into a project setup for URP or HDRP.
2. If using HDRP, from package manager, import High Definition Render Pipeline 14.0.10 (U2024.3.24 LTS+)
3. If using URP, from the package manager, import Universal Render Pipeline 14.0.10 or newer (U2024.3.24 LTS+)
4. From the Unity Editor double-click** on either the SSC_URP_[version] or SSC_HDRP_[version] package within this folder
5. TechDemo has its own packages, SSC_TechDemo_URP_[version] or SSC_TechDemo_HDRP_[version]. These require U2024.3.24+ or newer.
6. TechDemo3 has its own packages, SSC_TechDemo3_URP_[version] or SSC_TechDemo3_HDRP_[version]. These require U2024.3.24+ or newer.

** If double-click does not work, from the Unity Editor menu, Assets/Import Package/Custom Package... navigate to the folder within your project where the package is located to import the package.

In this release stars will not be visible in the TechDemo demos unless the built-in or URP 14.0.10+ pipelines are used.

For HDRP you may need to lower the Sun Lux from say 120000 to 80000 and reduce the Sky Exposure from say 16 to 14 in some of the demo scenes. Adjust values to suit your tastes.