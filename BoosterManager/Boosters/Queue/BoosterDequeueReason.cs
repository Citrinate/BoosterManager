namespace BoosterManager {
	internal enum BoosterDequeueReason {
		AlreadyQueued,
		Crafted,
		RemovedByUser,
		Uncraftable,
		UnexpectedlyUncraftable,
		Unmarketable,
		JobStopped
	}
}
