using UnityEngine;
using GameBalap.Core;

namespace GameBalap.Gameplay.Player
{
    /// <summary>
    /// Membangun model karakter 3D bergaya balok (Roblox / Minecraft style) secara prosedural
    /// dan menggerakkan animasi dinamis (lari atletis, kemiringan badan saat belok, squash & stretch lompat/mendarat, slide keren).
    /// </summary>
    public class BlockyCharacterAnimator : MonoBehaviour
    {
        [Header("Appearance Colors")]
        [SerializeField] private Color skinColor = new Color(0.98f, 0.82f, 0.68f);
        [SerializeField] private Color shirtColor = new Color(0.12f, 0.55f, 0.95f);
        [SerializeField] private Color pantsColor = new Color(0.15f, 0.18f, 0.25f);
        [SerializeField] private Color hairColor = new Color(0.25f, 0.15f, 0.08f);
        [SerializeField] private Color shoeColor = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] private bool isRival = false;

        // Limbs Transforms
        private Transform modelRoot;
        private Transform head;
        private Transform torso;
        private Transform leftArmPivot;
        private Transform rightArmPivot;
        private Transform leftLegPivot;
        private Transform rightLegPivot;

        // Dynamic State
        private CharacterMovementState currentState = CharacterMovementState.Running;
        private float runSpeedFactor = 1f;
        private float animTimer = 0f;
        private float currentLeanAngle = 0f;
        private float targetLeanAngle = 0f;
        private Vector3 currentScale = Vector3.one;

        public void SetupCharacter(bool asRival)
        {
            this.isRival = asRival;
            if (asRival)
            {
                shirtColor = new Color(0.92f, 0.22f, 0.22f); // Merah sporty untuk lawan
                pantsColor = new Color(0.12f, 0.12f, 0.15f);
                hairColor = new Color(0.1f, 0.1f, 0.1f);
            }

            BuildBlockyHumanoid();
        }

        public void ApplySkinColors(Color shirt, Color pants, Color hair, Color skin, Color shoes)
        {
            this.shirtColor = shirt;
            this.pantsColor = pants;
            this.hairColor = hair;
            this.skinColor = skin;
            this.shoeColor = shoes;

            BuildBlockyHumanoid();
        }

        private void BuildBlockyHumanoid()
        {
            Transform existing = transform.Find("BlockyModelRoot");
            if (existing != null) DestroyImmediate(existing.gameObject);

            GameObject rootObj = new GameObject("BlockyModelRoot");
            rootObj.transform.SetParent(transform, false);
            rootObj.transform.localPosition = Vector3.zero;
            modelRoot = rootObj.transform;

            Material skinMat = CreateMat("SkinMat", skinColor);
            Material shirtMat = CreateMat("ShirtMat", shirtColor);
            Material pantsMat = CreateMat("PantsMat", pantsColor);
            Material hairMat = CreateMat("HairMat", hairColor);
            Material shoeMat = CreateMat("ShoeMat", shoeColor);
            Material eyeMat = CreateMat("EyeMat", Color.black);
            Material headbandMat = CreateMat("HeadbandMat", isRival ? Color.yellow : Color.cyan);

            // 1. TORSO (Badan Utama Atletis)
            GameObject torsoObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torsoObj.name = "Torso";
            torsoObj.transform.SetParent(modelRoot, false);
            torsoObj.transform.localPosition = new Vector3(0, 1.15f, 0);
            torsoObj.transform.localScale = new Vector3(0.72f, 0.85f, 0.42f);
            StripCollider(torsoObj);
            torsoObj.GetComponent<MeshRenderer>().material = shirtMat;
            torso = torsoObj.transform;

            // Nomor Dada Atlet
            GameObject badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "ChestNumberBadge";
            badge.transform.SetParent(torso, false);
            badge.transform.localPosition = new Vector3(0, 0.1f, 0.52f);
            badge.transform.localScale = new Vector3(0.5f, 0.45f, 0.05f);
            StripCollider(badge);
            badge.GetComponent<MeshRenderer>().material = shoeMat;

            // 2. HEAD (Kepala Kotak Roblox/Minecraft)
            GameObject headObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headObj.name = "Head";
            headObj.transform.SetParent(modelRoot, false);
            headObj.transform.localPosition = new Vector3(0, 1.95f, 0);
            headObj.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            StripCollider(headObj);
            headObj.GetComponent<MeshRenderer>().material = skinMat;
            head = headObj.transform;

            // Rambut / Haircap
            GameObject hair = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hair.name = "Hair";
            hair.transform.SetParent(head, false);
            hair.transform.localPosition = new Vector3(0, 0.35f, -0.05f);
            hair.transform.localScale = new Vector3(1.04f, 0.38f, 1.05f);
            StripCollider(hair);
            hair.GetComponent<MeshRenderer>().material = hairMat;

            // Headband Sporty
            GameObject headband = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headband.name = "Headband";
            headband.transform.SetParent(head, false);
            headband.transform.localPosition = new Vector3(0, 0.15f, 0);
            headband.transform.localScale = new Vector3(1.05f, 0.18f, 1.05f);
            StripCollider(headband);
            headband.GetComponent<MeshRenderer>().material = headbandMat;

            // Mata Kiri & Kanan
            CreateFacialFeature(head, new Vector3(-0.16f, -0.02f, 0.52f), new Vector3(0.12f, 0.16f, 0.05f), eyeMat);
            CreateFacialFeature(head, new Vector3(0.16f, -0.02f, 0.52f), new Vector3(0.12f, 0.16f, 0.05f), eyeMat);

            // 3. TANGAN KIRI & KANAN (Pivot di Bahu)
            leftArmPivot = CreateLimb(modelRoot, "LeftArmPivot", new Vector3(-0.55f, 1.55f, 0),
                new Vector3(0, -0.42f, 0), new Vector3(0.28f, 0.85f, 0.32f), shirtMat, skinMat);

            rightArmPivot = CreateLimb(modelRoot, "RightArmPivot", new Vector3(0.55f, 1.55f, 0),
                new Vector3(0, -0.42f, 0), new Vector3(0.28f, 0.85f, 0.32f), shirtMat, skinMat);

            // 4. KAKI KIRI & KANAN (Pivot di Pinggul)
            leftLegPivot = CreateLimb(modelRoot, "LeftLegPivot", new Vector3(-0.20f, 0.75f, 0),
                new Vector3(0, -0.38f, 0), new Vector3(0.30f, 0.75f, 0.34f), pantsMat, shoeMat);

            rightLegPivot = CreateLimb(modelRoot, "RightLegPivot", new Vector3(0.20f, 0.75f, 0),
                new Vector3(0, -0.38f, 0), new Vector3(0.30f, 0.75f, 0.34f), pantsMat, shoeMat);
        }

        private Transform CreateLimb(Transform parent, string name, Vector3 pivotPos, Vector3 meshOffset, Vector3 meshScale, Material topMat, Material botMat)
        {
            GameObject pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = pivotPos;

            GameObject limbMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            limbMesh.name = "Mesh";
            limbMesh.transform.SetParent(pivot.transform, false);
            limbMesh.transform.localPosition = meshOffset;
            limbMesh.transform.localScale = meshScale;
            StripCollider(limbMesh);
            limbMesh.GetComponent<MeshRenderer>().material = topMat;

            GameObject botLimb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            botLimb.name = "LowerPart";
            botLimb.transform.SetParent(limbMesh.transform, false);
            botLimb.transform.localPosition = new Vector3(0, -0.35f, 0);
            botLimb.transform.localScale = new Vector3(1.02f, 0.35f, 1.05f);
            StripCollider(botLimb);
            botLimb.GetComponent<MeshRenderer>().material = botMat;

            return pivot.transform;
        }

        private void CreateFacialFeature(Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            StripCollider(obj);
            obj.GetComponent<MeshRenderer>().material = mat;
        }

        private Material CreateMat(string name, Color col)
        {
            return new Material(Shader.Find("Standard"))
            {
                name = name,
                color = col
            };
        }

        private void StripCollider(GameObject obj)
        {
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
        }

        public void UpdateAnimation(CharacterMovementState state, float speed, float horizontalDelta = 0f)
        {
            this.currentState = state;
            this.runSpeedFactor = Mathf.Max(0.5f, speed / GameConstants.DEFAULT_RUN_SPEED);

            // Hitung kemiringan badan saat berpindah jalur (Bank Angle)
            targetLeanAngle = Mathf.Clamp(-horizontalDelta * 2.8f, -18f, 18f);
        }

        private void Update()
        {
            if (modelRoot == null) return;

            animTimer += Time.deltaTime * runSpeedFactor * 14f;

            // Interpolasi halus kemiringan badan saat bermanuver belok
            currentLeanAngle = Mathf.Lerp(currentLeanAngle, targetLeanAngle, Time.deltaTime * 12f);

            switch (currentState)
            {
                case CharacterMovementState.Running:
                    AnimateRunning();
                    break;
                case CharacterMovementState.Jumping:
                    AnimateJumping();
                    break;
                case CharacterMovementState.Sliding:
                    AnimateSliding();
                    break;
                case CharacterMovementState.Stumbled:
                    AnimateStumble();
                    break;
                case CharacterMovementState.Finished:
                    AnimateVictory();
                    break;
            }

            // Kembalikan scale squash & stretch ke normal secara elastis
            modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, currentScale, Time.deltaTime * 14f);
        }

        private void AnimateRunning()
        {
            currentScale = Vector3.one;

            // Ayunan dinamis ekstrem dengan sudut 50 derajat
            float legSwingAngle = Mathf.Sin(animTimer) * 50f;
            float armSwingAngle = -legSwingAngle * 1.2f;
            float torsoBob = Mathf.Abs(Mathf.Sin(animTimer * 2f)) * 0.1f;
            float torsoTwist = Mathf.Sin(animTimer) * 12f; // Counter-rotation dada terhadap pinggul

            if (leftLegPivot != null) leftLegPivot.localRotation = Quaternion.Euler(legSwingAngle, 0, 0);
            if (rightLegPivot != null) rightLegPivot.localRotation = Quaternion.Euler(-legSwingAngle, 0, 0);

            // Ayunan lengan bersudut atletis
            if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(armSwingAngle, 0, -8f);
            if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(-armSwingAngle, 0, 8f);

            // Badan bergoyang atas-bawah dan memutar sedikit
            if (torso != null)
            {
                torso.localPosition = new Vector3(0, 1.15f + torsoBob, 0);
                torso.localRotation = Quaternion.Euler(0, torsoTwist, 0);
            }

            if (head != null)
            {
                head.localRotation = Quaternion.Euler(torsoBob * 35f, -torsoTwist * 0.6f, 0);
            }

            // Miringkan badan saat belok jalur
            modelRoot.localRotation = Quaternion.Euler(0, 0, currentLeanAngle);
        }

        private void AnimateJumping()
        {
            // Pose melompat (Stretch: badan memanjang sedikit ke atas)
            currentScale = new Vector3(0.92f, 1.15f, 0.92f);

            if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(-145f, 0, -25f);
            if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(-145f, 0, 25f);
            if (leftLegPivot != null) leftLegPivot.localRotation = Quaternion.Euler(35f, 0, -12f);
            if (rightLegPivot != null) rightLegPivot.localRotation = Quaternion.Euler(-18f, 0, 12f);
            if (head != null) head.localRotation = Quaternion.Euler(-18f, 0, 0);

            modelRoot.localRotation = Quaternion.Euler(-8f, 0, currentLeanAngle);
        }

        private void AnimateSliding()
        {
            // Pose slide: badan rebah dinamis, tangan kanan menyentuh aspal, kaki menjulur lurus
            currentScale = new Vector3(1.08f, 0.75f, 1.08f);

            modelRoot.localRotation = Quaternion.Lerp(modelRoot.localRotation, Quaternion.Euler(-60f, 0, 12f + currentLeanAngle), Time.deltaTime * 16f);
            if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(30f, 0, -40f);
            if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(60f, 0, 20f);
            if (leftLegPivot != null) leftLegPivot.localRotation = Quaternion.Euler(-38f, 0, 0);
            if (rightLegPivot != null) rightLegPivot.localRotation = Quaternion.Euler(-38f, 0, 0);
        }

        private void AnimateStumble()
        {
            currentScale = Vector3.one;

            float wobble = Mathf.Sin(Time.time * 26f) * 22f;
            modelRoot.localRotation = Quaternion.Euler(18f, wobble, wobble * 0.9f);
            if (head != null) head.localRotation = Quaternion.Euler(14f, -wobble * 1.8f, 0);
            if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(-40f + wobble, 0, -50f);
            if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(-40f - wobble, 0, 50f);
        }

        private void AnimateVictory()
        {
            currentScale = Vector3.one;

            float wave = Mathf.Sin(Time.time * 8f) * 38f;
            float jumpBob = Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.28f;

            modelRoot.localPosition = new Vector3(0, jumpBob, 0);
            modelRoot.localRotation = Quaternion.identity;

            if (leftArmPivot != null) leftArmPivot.localRotation = Quaternion.Euler(-165f + wave, 0, -25f);
            if (rightArmPivot != null) rightArmPivot.localRotation = Quaternion.Euler(-165f - wave, 0, 25f);
            if (leftLegPivot != null) leftLegPivot.localRotation = Quaternion.identity;
            if (rightLegPivot != null) rightLegPivot.localRotation = Quaternion.identity;
            if (head != null) head.localRotation = Quaternion.Euler(-10f, Mathf.Sin(Time.time * 4f) * 15f, 0);
        }
    }
}
