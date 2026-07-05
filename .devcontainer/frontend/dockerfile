FROM mcr.microsoft.com/devcontainers/javascript-node:22-bookworm

# Suppress Corepack's "about to download" prompt during yarn activation
ENV COREPACK_ENABLE_DOWNLOAD_PROMPT=0

# Enable Corepack so the Yarn version from package.json's packageManager field is used
RUN corepack enable

# Install bubblewrap (sandbox dependency for Codex CLI)
RUN apt-get update && apt-get install -y bubblewrap \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Playwright's OS dependencies for Chromium (version-independent apt packages).
# The browser binary itself is downloaded in postCreateCommand so it matches the
# Playwright version resolved from yarn.lock.
RUN npx -y playwright install-deps chromium \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install GitHub Copilot CLI and Codex CLI
RUN npm install -g @github/copilot @openai/codex

# Install Claude Code (as node user so it installs to their home directory)
RUN su - node -c "curl -fsSL https://claude.ai/install.sh | bash"

# Install zsh plugins for better shell experience
RUN git clone --depth=1 https://github.com/zsh-users/zsh-autosuggestions /home/node/.oh-my-zsh/custom/plugins/zsh-autosuggestions \
    && git clone --depth=1 https://github.com/zsh-users/zsh-syntax-highlighting /home/node/.oh-my-zsh/custom/plugins/zsh-syntax-highlighting \
    && chown -R node:node /home/node/.oh-my-zsh/custom/plugins/zsh-autosuggestions \
    && chown -R node:node /home/node/.oh-my-zsh/custom/plugins/zsh-syntax-highlighting

# Configure Oh My Zsh: agnoster theme, useful plugins, claude-d alias
RUN sed -i 's/ZSH_THEME=".*"/ZSH_THEME="agnoster"/' /home/node/.zshrc \
    && sed -i 's/plugins=(.*)/plugins=(git gh node npm docker zsh-autosuggestions zsh-syntax-highlighting)/' /home/node/.zshrc \
    && echo 'export PATH="$HOME/.local/bin:$PATH"' >> /home/node/.zshrc \
    && echo 'alias claude-d="claude --dangerously-skip-permissions"' >> /home/node/.zshrc \
    && echo 'alias codex-a="codex --full-auto"' >> /home/node/.zshrc \
    && echo 'alias codex-d="codex --dangerously-bypass-approvals-and-sandbox"' >> /home/node/.zshrc \
    && echo 'DEFAULT_USER=$USER' >> /home/node/.zshrc

# Set zsh as default shell
RUN chsh -s /bin/zsh node
