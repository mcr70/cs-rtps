A port of jRTPS to .NET
=======================
 There are some improvements attempted during porting of the the codebase.
 While not trying to be exhaustive, some of them are listed here

   - Attempting to stick with only one Marshaller. There should still
     be a possibility to extend Marshaller, so that in case of
     inadequate implementation, one can provide a custom implementation.
     Start with attributes (annotations) being mandatory

   - Better separation of rtps and udds layer. Rtps layer should
     not work with QualityOfService class. Instead, provide mandatory
     input to entities while constructing them. Like reliable or not.

   - Redesign history cache.
