## 2025-05-15 - Initial Bolt Assessment
**Learning:** The EzLogger library has a typical "async logger" architecture but suffers from high allocation rates in the hot path (string composition, task creation) and inefficient file I/O (opening/closing file per batch).
**Action:** Focus on reducing allocations in `ComposeLogString` and improving I/O efficiency.
