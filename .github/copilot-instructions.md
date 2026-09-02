# Copilot Instructions

## Project Guidelines
- In the Midden project, the CLI is designed for researchers (non-technical users), so design decisions should favor transparency and ease of use over strict security best practices. Also, "Mippen" (e.g. MippenFileSearchTerm) is intentional domain terminology, not a typo for "Midden".

## Code Quality
- Do not use arbitrary time delays (e.g. Task.Delay) or similar timing-based workarounds to fix race conditions in this project; find and fix the actual root cause with a deterministic solution (e.g. use lifecycle/event hooks instead).