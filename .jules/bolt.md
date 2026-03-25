# Bolt's Performance Journal

## 2025-03-25 - Initial Performance Review of EzLogger
**Learning:** The `EzLogger` singleton access uses a full `lock(lockObject)` on every log call, which creates significant contention and serializes log production in multi-threaded environments. Additionally, batch processing for console and file output involves multiple string allocations per log line (due to `StringBuilder.ToString()`), which can be avoided by using `StringBuilder` chunks or `Span<char>`.

**Action:** Implement `Lazy<T>` for the singleton and refactor the log composition to be allocation-free by using shared `StringBuilder` buffers.
