using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace FirstLevel.EditorTests
{
    public sealed class ObstacleTests
    {
        [Test]
        public void StartAppliesRandomizedSizeWithinConfiguredRange()
        {
            GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
            SetPublicField(obstacle, "minSize", 1.25f);
            SetPublicField(obstacle, "maxSize", 1.25f);

            obstacle.SendMessage("Start");

            Assert.That(obstacleObject.transform.localScale, Is.EqualTo(new Vector3(1.25f, 1.25f, 1f)));
            Object.DestroyImmediate(obstacleObject);
        }

        [Test]
        public void PulseScalesObstacleFromBaseScale()
        {
            GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
            SetPublicField(obstacle, "pulseScale", true);
            SetPublicField(obstacle, "pulseAmount", 0.25f);
            SetPublicField(obstacle, "pulseSpeed", 1f);
            SetPrivateField(obstacle, "baseScale", Vector3.one);
            SetPrivateField(obstacle, "spawnTime", Time.time - 0.25f);

            InvokePrivateMethod(obstacle, "Pulse");

            Assert.That(obstacleObject.transform.localScale.x, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(obstacleObject.transform.localScale.y, Is.EqualTo(1.25f).Within(0.0001f));
            Object.DestroyImmediate(obstacleObject);
        }

        [Test]
        public void WrapAroundCameraMovesObstacleToOppositeHorizontalEdge()
        {
            using (CreateCamera(out Camera camera))
            {
                GameObject obstacleObject = CreateObstacle(out MonoBehaviour obstacle);
                SetPublicField(obstacle, "wrapAroundCameraBounds", true);
                SetPublicField(obstacle, "boundaryCamera", camera);
                SetPublicField(obstacle, "wrapMargin", 0.5f);
                obstacleObject.transform.position = new Vector3(-6f, 0f, 0f);

                InvokePrivateMethod(obstacle, "WrapAroundCamera");

                Assert.That(obstacleObject.transform.position.x, Is.EqualTo(5.5f).Within(0.0001f));
                Object.DestroyImmediate(obstacleObject);
            }
        }

        private static GameObject CreateObstacle(out MonoBehaviour obstacle)
        {
            var obstacleObject = new GameObject("Obstacle");
            obstacleObject.AddComponent<Rigidbody2D>();
            obstacleObject.AddComponent<SpriteRenderer>();
            obstacle = (MonoBehaviour)obstacleObject.AddComponent(GetObstacleType());
            SetPublicField(obstacle, "minSpeed", 0f);
            SetPublicField(obstacle, "maxSpeed", 0f);
            SetPublicField(obstacle, "maxSpinSpeed", 0f);
            SetPublicField(obstacle, "pulseScale", false);
            SetPublicField(obstacle, "wrapAroundCameraBounds", false);
            return obstacleObject;
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

        private static void SetPublicField<T>(MonoBehaviour obstacle, string fieldName, T value)
        {
            FieldInfo field = obstacle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            field.SetValue(obstacle, value);
        }

        private static void SetPrivateField<T>(MonoBehaviour obstacle, string fieldName, T value)
        {
            FieldInfo field = obstacle.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(obstacle, value);
        }

        private static void InvokePrivateMethod(MonoBehaviour obstacle, string methodName)
        {
            MethodInfo method = obstacle.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(obstacle, null);
        }

        private static System.Type GetObstacleType()
        {
            System.Type obstacleType = System.Type.GetType("Obstacle, Assembly-CSharp");
            Assert.That(obstacleType, Is.Not.Null);
            return obstacleType;
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
