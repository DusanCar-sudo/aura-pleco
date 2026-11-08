#!/usr/bin/env node
// Thin npm wrapper: runs the platform-native Pleco, prints install hints if missing.
const { spawnSync } = require('child_process');
const path = require('path');
const os = require('os');
const fs = require('fs');

const args = process.argv.slice(2);
const pkg = path.join(__dirname, '..', 'packages');

if (process.platform === 'win32') {
  const exe = path.join(process.env.LOCALAPPDATA || '', 'Pleco', 'pleco.exe');
  if (fs.existsSync(exe)) {
    const r = spawnSync(exe, args, { stdio: 'inherit' });
    process.exit(r.status || 0);
  }
  console.log('Pleco for Windows is not installed yet.');
  console.log('Run the install.bat from this package: packages\\windows\\install.bat (PowerShell as Administrator)');
  process.exit(1);
}

const script = path.join(pkg, 'linux', 'aura-pleco');
if (!fs.existsSync(script)) {
  console.error('aura-pleco: platform script missing from package');
  process.exit(1);
}
const r = spawnSync('bash', [script, ...args], { stdio: 'inherit' });
process.exit(r.status || 0);
