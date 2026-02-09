using Unity.Entities;

namespace Pixel
{
    public static class EntitiManagerUtil
    {
        public static T GetSingletonComponent<T>(this EntityManager em) where T : unmanaged, IComponentData
        {
            return em.CreateEntityQuery(typeof(T)).GetSingleton<T>();
        }

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em,bool isReadOnly=false) where T : unmanaged, IBufferElementData
        {
            return em.CreateEntityQuery(typeof(T)).GetSingletonBuffer<T>(isReadOnly);
        }
    }
}