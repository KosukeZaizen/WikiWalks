export async function sleepAsync(milliSec) {
    return new Promise(resolve => setTimeout(() => resolve(), milliSec));
}
