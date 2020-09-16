Pipeline logic is copied directly from https://github.com/Vycka/LoadRunner/tree/master/src/Viki.LoadRunner.Engine/Core/Collector/Pipeline to avoid currently linking it as dependency.
* Its a custom tool intended in performance efficient environment to move huge amounts of data from one thread to another, but it has limitations:
  - At any given time there can only be max 1 consumer and max 1 producer. (writing or reading from two threads at the same time is not thread safe.)
  - Order is not guaranteed (while designed with intention that ordering is not needed. End result (while not validated) seems to keep order intact)
  - Internal RoS/WoS lists will scale indefinetely. so dont forget to consume it.
  - Main bottleneck here is whether you will be able to consume stuff fast enough :)

At some point this utility should be presented as a separate package.