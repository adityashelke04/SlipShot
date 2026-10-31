# Preview footer refinement

The folder shortcut is an infrequent utility action. It now sits in a small bordered box immediately after “Auto-hide: 10s · Latest in tray”. The four main actions return to one segmented row: Edit / Copy / Save / Pin. Edit alone keeps the accent treatment.

The footer uses a 28 × 28 DIP folder button, matching the preview's existing dismiss control. This desktop pointer target is deliberately smaller than a 44-DIP touch control to preserve the compact footer hierarchy. Its existing native button template supplies keyboard focus and hover feedback; its accessible name is “Open file location” and tooltip is “Open captures folder”. The icon uses the established vector glyph style and theme colors, including high contrast. Retention text can trim on unusually narrow monitors while the folder control keeps its target size.

The callback, inline folder-open error handling and timer refresh remain unchanged. The shortcut neither saves nor edits the image, and remains enabled before and after the Save button changes to Saved. Async copy/save guards and document change subscriptions are untouched.

Regression coverage inspects an unshown logical tree for footer placement, the four main actions and target dimensions, then exercises synthetic callbacks before/after saved state and after a folder error. No native input, desktop capture, clipboard access or Explorer launch is needed. Build/test execution and synthetic visual rendering are owned by the coordinating agent; they were not run by this editing worker.
