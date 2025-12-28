
extern "C"
{
    void BurstRegisterStaticMethodDelegates(void *initialize, void *lookup) __attribute__((weak));

    void Staticburst_initialize(void *);
    void *StaticBurstStaticMethodLookup(void *);
}

__attribute__((constructor))
static void BurstSetup()
{
    if (BurstRegisterStaticMethodDelegates != nullptr)
    {
        BurstRegisterStaticMethodDelegates((void *)Staticburst_initialize, (void *)StaticBurstStaticMethodLookup);
    }
    else
    {
        fprintf(stderr, "Warning: You need a newer version of Unity to run Burst compiled code in the vision simulator. Burst is disabled.");
    }
}
