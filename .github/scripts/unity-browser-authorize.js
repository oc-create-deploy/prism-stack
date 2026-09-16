const { chromium } = require('playwright');

(async () => {
  const statePath = process.argv[2];
  const signInUrl = process.argv[3];
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({ storageState: statePath });
  const page = await context.newPage();

  try {
    await page.goto(signInUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
  } catch (error) {
    if (!/ERR_NAME_NOT_RESOLVED|ERR_ABORTED/.test(error.message)) throw error;
  }

  for (let attempt = 0; attempt < 60; attempt += 1) {
    if (page.url().includes('/security-check')) {
      throw new Error('Unity requested a new email security check');
    }

    const button = page.getByRole('button', {
      name: /authorize|allow|accept|continue|sign in|open unity/i,
    }).first();
    if (await button.count() && await button.isVisible()) {
      await button.click();
    }

    const text = await page.locator('body').innerText().catch(() => '');
    if (/success|authorized|you may close|return to unity/i.test(text)) break;
    await page.waitForTimeout(1000);
  }

  await context.storageState({ path: statePath });
  await browser.close();
  console.log('Unity browser authorization completed');
})().catch(error => {
  console.error(error.message);
  process.exit(1);
});
