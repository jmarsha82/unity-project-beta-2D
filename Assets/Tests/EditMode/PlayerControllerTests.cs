using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FirstLevel.EditorTests
{
    public sealed class PlayerControllerTests
    {
        [Test]
        public void WrapAroundCameraMovesPlayerToOppositeHorizontalEdge()
        {
            using (CreateCamera(out Camera camera))
            {
                GameObject playerObject = CreatePlayer(out MonoBehaviour player);
                SetPrivateField(player, "cachedCamera", camera);
                SetPublicField(player, "wrapAroundCameraBounds", true);
                SetPublicField(player, "wrapMargin", 0.5f);
                playerObject.transform.position = new Vector3(-6f, 0f, 0f);

                InvokePrivateMethod(player, "WrapAroundCamera");

                Assert.That(playerObject.transform.position.x, Is.EqualTo(5.5f).Within(0.0001f));
                Object.DestroyImmediate(playerObject);
            }
        }

        [Test]
        public void ExplosionDisablesShipRenderingAndPhysics()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            player.SendMessage("Start");

            InvokePrivateMethod(player, "ExplodeAndDestroy");

            Assert.That(playerObject.GetComponent<Rigidbody2D>().simulated, Is.False);
            Assert.That(playerObject.GetComponentInChildren<SpriteRenderer>().enabled, Is.False);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void ShieldStateReportsActiveBeforeEndTime()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPrivateField(player, "shieldEndTime", Time.time + 1f);

            bool isShieldActive = (bool)InvokePrivateMethod(player, "IsShieldActive");

            Assert.That(isShieldActive, Is.True);
            Object.DestroyImmediate(playerObject);
        }

        private static GameObject CreatePlayer(out MonoBehaviour player)
        {
            var playerObject = new GameObject("Player");
            playerObject.AddComponent<Rigidbody2D>();

            var hull = new GameObject("Life_Support");
            hull.transform.SetParent(playerObject.transform, false);
            hull.AddComponent<SpriteRenderer>();

            player = (MonoBehaviour)playerObject.AddComponent(GetPlayerControllerType());
            SetPublicField(player, "stylizeShipOnStart", false);
            SetPublicField(player, "explosionFragments", 1);
            SetPublicField(player, "destroyDelay", 10f);
            return playerObject;
        }

        private static DisposableCamera CreateCamera(out Camera camera)
        {
            var cameraObject = new GameObject("Boundary Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.aspect = 1f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return new DisposableCamera(cameraObject);
        }

        private static void SetPublicField<T>(MonoBehaviour component, string fieldName, T value)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            field.SetValue(component, value);
        }

        private static void SetPrivateField<T>(MonoBehaviour component, string fieldName, T value)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(component, value);
        }

        private static object InvokePrivateMethod(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(component, null);
        }

        private static System.Type GetPlayerControllerType()
        {
            System.Type playerControllerType = System.Type.GetType("PlayerController, Assembly-CSharp");
            Assert.That(playerControllerType, Is.Not.Null);
            return playerControllerType;
        }

        private readonly struct DisposableCamera : System.IDisposable
        {
            private readonly GameObject cameraObject;

            public DisposableCamera(GameObject cameraObject)
            {
                this.cameraObject = cameraObject;
            }

            public void Dispose()
            {
                Object.DestroyImmediate(cameraObject);
            }
        }
    }
}
