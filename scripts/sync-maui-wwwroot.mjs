import { cpSync, existsSync, mkdirSync, readdirSync, rmSync } from "node:fs";
import { join, resolve } from "node:path";

const rootDir = resolve(".");
const distDir = join(rootDir, "dist");
const mauiWwwrootDir = join(rootDir, "blazor", "blazorApp", "blazorApp", "wwwroot");

if (!existsSync(distDir)) {
  throw new Error("Missing dist directory. Run the Vite build first.");
}

if (!existsSync(mauiWwwrootDir)) {
  mkdirSync(mauiWwwrootDir, { recursive: true });
}

for (const entry of readdirSync(mauiWwwrootDir, { withFileTypes: true })) {
  rmSync(join(mauiWwwrootDir, entry.name), { recursive: true, force: true });
}

for (const entry of readdirSync(distDir, { withFileTypes: true })) {
  cpSync(join(distDir, entry.name), join(mauiWwwrootDir, entry.name), {
    recursive: true,
  });
}

console.log(`Synced Vite build output to ${mauiWwwrootDir}`);
