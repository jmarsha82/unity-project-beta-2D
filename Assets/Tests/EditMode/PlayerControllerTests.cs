using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

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

        [Test]
        public void FiringLaserCreatesProjectileInFrontOfPlayer()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "canFireLasers", true);
            SetPublicField(player, "laserCooldown", 0f);
            SetPrivateField(player, "thrustDirection", Vector2.up);

            InvokePrivateMethod(player, "TryFireLaser");

            GameObject laser = GameObject.Find("Player Laser");
            Assert.That(laser, Is.Not.Null);
            Assert.That(laser.GetComponent<BoxCollider2D>(), Is.Not.Null);
            Assert.That(laser.GetComponent<Rigidbody2D>().linearVelocity.y, Is.GreaterThan(0f));
            Object.DestroyImmediate(laser);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void AddScoreIncreasesScoreWithoutUiDocument()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "scoreMultiplier", 0f);
            player.SendMessage("Start");

            InvokePublicMethod(player, "AddScore", 5);

            Assert.That(GetPublicField<float>(player, "score"), Is.EqualTo(5f));
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void ScoreCombinesElapsedTimeAndBonusPoints()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "scoreMultiplier", 10f);
            SetPublicField(player, "elapsedTime", 2.4f);
            player.SendMessage("Start");

            InvokePublicMethod(player, "AddScore", 5);

            Assert.That(GetPublicField<float>(player, "score"), Is.EqualTo(29f));
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void UpdateScoreAdvancesElapsedTimeScore()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "scoreMultiplier", 10f);
            player.SendMessage("Start");

            InvokePrivateMethod(player, "UpdateScore", 1.25f);

            Assert.That(GetPublicField<float>(player, "elapsedTime"), Is.EqualTo(1.25f));
            Assert.That(GetPublicField<float>(player, "score"), Is.EqualTo(12f));
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void StartCreatesHighScorePlayerPrefsWhenMissing()
        {
            string prefsKey = "PlayerControllerTests.HighScore.Missing";
            PlayerPrefs.DeleteKey(prefsKey);

            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "highScorePlayerPrefsKey", prefsKey);

            player.SendMessage("Start");

            Assert.That(PlayerPrefs.HasKey(prefsKey), Is.True);
            Assert.That(PlayerPrefs.GetInt(prefsKey), Is.EqualTo(0));
            Assert.That(GetPublicField<int>(player, "highScore"), Is.EqualTo(0));
            PlayerPrefs.DeleteKey(prefsKey);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void GameEndSavesNewHighScore()
        {
            string prefsKey = "PlayerControllerTests.HighScore.New";
            PlayerPrefs.SetInt(prefsKey, 12);

            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "highScorePlayerPrefsKey", prefsKey);
            SetPublicField(player, "scoreMultiplier", 0f);
            player.SendMessage("Start");
            InvokePublicMethod(player, "AddScore", 30);

            InvokePrivateMethod(player, "ExplodeAndDestroy");

            Assert.That(PlayerPrefs.GetInt(prefsKey), Is.EqualTo(30));
            Assert.That(GetPublicField<int>(player, "highScore"), Is.EqualTo(30));
            PlayerPrefs.DeleteKey(prefsKey);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void GameEndKeepsExistingHighScoreWhenCurrentScoreIsLower()
        {
            string prefsKey = "PlayerControllerTests.HighScore.Keep";
            PlayerPrefs.SetInt(prefsKey, 50);

            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            SetPublicField(player, "highScorePlayerPrefsKey", prefsKey);
            SetPublicField(player, "scoreMultiplier", 0f);
            player.SendMessage("Start");
            InvokePublicMethod(player, "AddScore", 20);

            InvokePrivateMethod(player, "ExplodeAndDestroy");

            Assert.That(PlayerPrefs.GetInt(prefsKey), Is.EqualTo(50));
            Assert.That(GetPublicField<int>(player, "highScore"), Is.EqualTo(50));
            PlayerPrefs.DeleteKey(prefsKey);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void RefreshHighScoreLabelDisplaysSavedValue()
        {
            GameObject playerObject = CreatePlayer(out MonoBehaviour player);
            var highScoreLabel = new Label();
            SetPrivateField(player, "highScoreText", highScoreLabel);
            SetPublicField(player, "highScore", 42);

            InvokePrivateMethod(player, "RefreshHighScoreLabel");

            Assert.That(highScoreLabel.text, Is.EqualTo("High Score: 42"));
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

        private static T GetPublicField<T>(MonoBehaviour component, string fieldName)
        {
            FieldInfo field = component.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(component);
        }

        private static object InvokePrivateMethod(MonoBehaviour component, string methodName)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(component, null);
        }

        private static object InvokePrivateMethod(MonoBehaviour component, string methodName, params object[] arguments)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return method.Invoke(component, arguments);
        }

        private static void InvokePublicMethod(MonoBehaviour component, string methodName, params object[] arguments)
        {
            MethodInfo method = component.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            method.Invoke(component, arguments);
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
