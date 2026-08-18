using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using BackEnd;
using LitJson;
using UnityEngine;

namespace MultiCharacterSample.Data
{
    /// <summary>
    /// 뒤끝 멀티 캐릭터와 UserData 게임 정보를 관리한다.
    /// 계정 비밀번호는 캐릭터에서 계정 컨텍스트로 돌아갈 때만 필요하며 메모리에만 유지한다.
    /// </summary>
    public static class CharacterRepository
    {
        const string TableName = "UserData";

        static readonly List<CharacterData> characters = new List<CharacterData>();
        static readonly Dictionary<string, int> pendingPortraits = new Dictionary<string, int>();

        static string accountId;
        static string accountPassword;
        static string lastSelectedId;
        static CharacterData selectedCharacter;
        static int revision;
        static int savedRevision;
        static bool saveInProgress;

        public static List<CharacterData> Characters { get { return characters; } }
        public static bool IsAccountReady { get { return Backend.IsMultiAccountLogin; } }
        public static bool IsElevationRequired { get { return Backend.IsLogin && Backend.NeedsElevation; } }
        public static bool IsProjectChangeRequired { get { return Backend.IsLogin && !Backend.NeedsElevation && !Backend.IsMultiAccountLogin; } }
        public static bool NeedsSaveBeforeQuit { get { return selectedCharacter != null && (saveInProgress || revision != savedRevision); } }
        public static string LastSelectedId
        {
            get { return lastSelectedId; }
            set { lastSelectedId = value ?? string.Empty; }
        }

        public static CharacterData Get(string id)
        {
            return characters.Find(character => character.id == id);
        }

        public static IEnumerator LoginOrRegister(string id, string password, Action<bool, string> completed)
        {
            bool initialized = false;
            string initializationError = null;
            yield return Initialize((success, error) =>
            {
                initialized = success;
                initializationError = error;
            });

            if (!initialized)
            {
                completed(false, initializationError);
                yield break;
            }

            accountId = id;
            accountPassword = password;

            bool authenticated = false;
            string authenticationError = null;
            yield return Authenticate(true, (success, error) =>
            {
                authenticated = success;
                authenticationError = error;
            });

            if (authenticated)
            {
                characters.Clear();
                pendingPortraits.Clear();
                lastSelectedId = string.Empty;
                selectedCharacter = null;
            }

            completed(authenticated, authenticationError);
        }

        public static IEnumerator ElevateToMultiCharacter(Action<bool, string> completed)
        {
            if (!Backend.IsInitialized || !Backend.IsLogin)
            {
                completed(false, "계정 로그인이 필요합니다.");
                yield break;
            }

            if (!Backend.NeedsElevation)
            {
                completed(Backend.IsMultiAccountLogin, Backend.IsMultiAccountLogin ? null : "멀티 캐릭터 계정 전환이 필요합니다.");
                yield break;
            }

            BackendReturnObject result = null;
            Backend.BMember.Elevate(accountId, accountPassword, response => result = response);
            yield return new WaitUntil(() => result != null);

            if (!result.IsSuccess() && result.GetStatusCode() != "403")
            {
                Debug.LogError($"[CharacterRepository] 멀티 계정 전환 실패: {result.GetMessage()}");
                completed(false, "멀티 캐릭터 계정 전환에 실패했습니다.");
                yield break;
            }

            if (!Backend.IsMultiAccountLogin)
            {
                completed(false, "멀티 캐릭터 계정으로 로그인되지 않았습니다.");
                yield break;
            }

            characters.Clear();
            pendingPortraits.Clear();
            lastSelectedId = string.Empty;
            selectedCharacter = null;
            completed(true, null);
        }

        static IEnumerator Initialize(Action<bool, string> completed)
        {
            if (Backend.IsInitialized)
            {
                completed(true, null);
                yield break;
            }

            BackendReturnObject result = null;
            Backend.InitializeAsync(response => result = response);
            yield return new WaitUntil(() => result != null);

            if (!result.IsSuccess())
            {
                Debug.LogError($"[CharacterRepository] Backend.Initialize 실패: {result.GetMessage()}");
                completed(false, "서버 초기화에 실패했습니다.");
                yield break;
            }

            Debug.Log($"[CharacterRepository] Backend {Backend.__Version()} 초기화 완료");
            completed(true, null);
        }

        static IEnumerator Authenticate(bool allowRegistration, Action<bool, string> completed)
        {
            BackendReturnObject loginResult = null;
            Backend.BMember.CustomLogin(accountId, accountPassword, response => loginResult = response);
            yield return new WaitUntil(() => loginResult != null);

            if (!loginResult.IsSuccess())
            {
                if (!allowRegistration || !IsMissingCustomAccount(loginResult))
                {
                    Debug.LogWarning($"[CharacterRepository] CustomLogin 실패({loginResult.GetStatusCode()}): {loginResult.GetMessage()}");
                    completed(false, DescribeLoginFailure(loginResult));
                    yield break;
                }

                BackendReturnObject signUpResult = null;
                Backend.BMember.CustomSignUp(accountId, accountPassword, response => signUpResult = response);
                yield return new WaitUntil(() => signUpResult != null);

                if (!signUpResult.IsSuccess())
                {
                    Debug.LogWarning($"[CharacterRepository] CustomSignUp 실패({signUpResult.GetStatusCode()}): {signUpResult.GetMessage()}");
                    completed(false, signUpResult.GetStatusCode() == "409"
                        ? "아이디 또는 비밀번호를 확인하세요."
                        : "회원가입에 실패했습니다.");
                    yield break;
                }

                loginResult = null;
                Backend.BMember.CustomLogin(accountId, accountPassword, response => loginResult = response);
                yield return new WaitUntil(() => loginResult != null);

                if (!loginResult.IsSuccess())
                {
                    Debug.LogError($"[CharacterRepository] 회원가입 후 CustomLogin 실패: {loginResult.GetMessage()}");
                    completed(false, "회원가입 후 로그인에 실패했습니다.");
                    yield break;
                }
            }

            if (Backend.NeedsElevation)
            {
                completed(false, "싱글 캐릭터 계정입니다.");
                yield break;
            }

            if (!Backend.IsMultiAccountLogin)
            {
                completed(false, "멀티 캐릭터 계정으로 로그인되지 않았습니다.");
                yield break;
            }

            completed(true, null);
        }

        static bool IsMissingCustomAccount(BackendReturnObject result)
        {
            string message = result.GetMessage() ?? string.Empty;
            return result.GetStatusCode() == "401" && message.StartsWith("bad customId", StringComparison.OrdinalIgnoreCase);
        }

        static string DescribeLoginFailure(BackendReturnObject result)
        {
            string message = result.GetMessage() ?? string.Empty;
            return message.StartsWith("bad customPassword", StringComparison.OrdinalIgnoreCase)
                ? "아이디 또는 비밀번호를 확인하세요."
                : "계정 로그인에 실패했습니다.";
        }

        public static IEnumerator LoadCharacters(Action<bool, string> completed)
        {
            if (!Backend.IsInitialized || !Backend.IsMultiAccountLogin)
            {
                completed(false, "멀티 캐릭터 계정 로그인이 필요합니다.");
                yield break;
            }

            if (!Backend.LocationProperties.IsLoadLocation)
            {
                BackendReturnObject locationResult = null;
                Backend.LocationProperties.UpdateLocationProperties(response => locationResult = response);
                yield return new WaitUntil(() => locationResult != null);

                if (!locationResult.IsSuccess() && locationResult.GetStatusCode() != "204")
                {
                    Debug.LogWarning($"[CharacterRepository] 위치 정보 조회 실패, 기본값 사용: {locationResult.GetMessage()}");
                    Backend.LocationProperties.CustomizeLocationProperties("Seoul", "KR", "Seoul", "ko-KR");
                }
            }

            BackendReturnObject result = null;
            Backend.MultiCharacter.Character.GetCharacterList(TableName, response => result = response);
            yield return new WaitUntil(() => result != null);

            if (!result.IsSuccess())
            {
                Debug.LogError($"[CharacterRepository] 캐릭터 목록 조회 실패: {result.GetMessage()}");
                completed(false, "캐릭터 목록을 불러오지 못했습니다.");
                yield break;
            }

            characters.Clear();
            JsonData json = result.GetReturnValuetoJSON();
            if (json != null && json.ContainsKey("characters"))
            {
                JsonData rows = json["characters"];
                for (int i = 0; i < rows.Count; i++)
                {
                    JsonData row = rows[i];
                    var character = new CharacterData
                    {
                        id = ReadString(row, "uuid", string.Empty),
                        inDate = ReadString(row, "inDate", string.Empty),
                        characterName = ReadString(row, "nickname", string.Empty),
                        portraitIndex = i
                    };

                    if (pendingPortraits.TryGetValue(character.id, out int portraitIndex))
                        character.portraitIndex = portraitIndex;
                    if (row.ContainsKey(TableName))
                    {
                        ApplyRow(character, row[TableName]);
                        pendingPortraits.Remove(character.id);
                    }

                    characters.Add(character);
                }
            }

            completed(true, null);
        }

        public static IEnumerator CreateCharacter(string name, int portraitIndex, Action<bool, string, string> completed)
        {
            BackendReturnObject result = null;
            Backend.MultiCharacter.Character.CreateCharacter(name, response => result = response);
            yield return new WaitUntil(() => result != null);

            if (!result.IsSuccess())
            {
                Debug.LogWarning($"[CharacterRepository] 캐릭터 생성 실패({result.GetStatusCode()}): {result.GetMessage()}");
                completed(false, result.GetStatusCode() == "409" ? "이미 사용 중인 이름입니다." : "캐릭터 생성에 실패했습니다.", null);
                yield break;
            }

            JsonData json = result.GetReturnValuetoJSON();
            string uuid = ReadString(json, "uuid", string.Empty);
            string inDate = ReadString(json, "gamerInDate", string.Empty);
            if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(inDate))
            {
                completed(false, "생성된 캐릭터 정보를 확인하지 못했습니다.", uuid);
                yield break;
            }

            pendingPortraits[uuid] = portraitIndex;
            lastSelectedId = uuid;

            BackendReturnObject selectResult = null;
            Backend.MultiCharacter.Character.SelectCharacter(uuid, inDate, response => selectResult = response);
            yield return new WaitUntil(() => selectResult != null);

            if (!selectResult.IsSuccess())
            {
                Debug.LogError($"[CharacterRepository] 생성 캐릭터 선택 실패: {selectResult.GetMessage()}");
                completed(false, "캐릭터는 생성됐지만 초기화하지 못했습니다.", uuid);
                yield break;
            }

            var character = new CharacterData
            {
                id = uuid,
                inDate = inDate,
                characterName = name,
                portraitIndex = portraitIndex
            };
            BackendReturnObject insertResult = null;
            Backend.GameData.Insert(TableName, BuildParam(character), response => insertResult = response);
            yield return new WaitUntil(() => insertResult != null);

            if (!insertResult.IsSuccess())
            {
                Debug.LogError($"[CharacterRepository] 생성 캐릭터 UserData 초기화 실패: {insertResult.GetMessage()}");
                bool restored = false;
                string restoreError = null;
                yield return ReturnToAccount((success, error) =>
                {
                    restored = success;
                    restoreError = error;
                });
                completed(false, restored ? "캐릭터는 생성됐지만 플레이 정보를 초기화하지 못했습니다." : restoreError, uuid);
                yield break;
            }

            bool accountReady = false;
            string accountError = null;
            yield return ReturnToAccount((success, error) =>
            {
                accountReady = success;
                accountError = error;
            });

            if (!accountReady)
            {
                completed(false, accountError, uuid);
                yield break;
            }

            completed(true, null, uuid);
        }

        public static IEnumerator DeleteCharacter(string id, Action<bool, string> completed)
        {
            CharacterData character = Get(id);
            if (character == null)
            {
                completed(false, "삭제할 캐릭터를 찾지 못했습니다.");
                yield break;
            }

            BackendReturnObject result = null;
            Backend.MultiCharacter.Character.DeleteCharacter(character.id, character.inDate, response => result = response);
            yield return new WaitUntil(() => result != null);

            if (!result.IsSuccess())
            {
                Debug.LogWarning($"[CharacterRepository] 캐릭터 삭제 실패: {result.GetMessage()}");
                completed(false, "캐릭터 삭제에 실패했습니다.");
                yield break;
            }

            characters.Remove(character);
            pendingPortraits.Remove(id);
            if (lastSelectedId == id) lastSelectedId = string.Empty;
            completed(true, null);
        }

        public static IEnumerator SelectAndLoadCharacter(string id, Action<bool, string, CharacterData> completed)
        {
            CharacterData character = Get(id);
            if (character == null)
            {
                completed(false, "선택한 캐릭터를 찾지 못했습니다.", null);
                yield break;
            }

            BackendReturnObject selectResult = null;
            Backend.MultiCharacter.Character.SelectCharacter(character.id, character.inDate, response => selectResult = response);
            yield return new WaitUntil(() => selectResult != null);

            if (!selectResult.IsSuccess())
            {
                Debug.LogWarning($"[CharacterRepository] 캐릭터 선택 실패: {selectResult.GetMessage()}");
                completed(false, "캐릭터 선택에 실패했습니다.", null);
                yield break;
            }

            BackendReturnObject readResult = null;
            Backend.GameData.GetMyData(TableName, new Where(), 1, response => readResult = response);
            yield return new WaitUntil(() => readResult != null);

            if (!readResult.IsSuccess())
            {
                Debug.LogError($"[CharacterRepository] UserData 조회 실패: {readResult.GetMessage()}");
                yield return RestoreAccountAfterCharacterFailure("플레이 정보를 불러오지 못했습니다.", completed);
                yield break;
            }

            JsonData rows = readResult.FlattenRows();
            if (rows != null && rows.Count > 0)
            {
                ApplyRow(character, rows[0]);
            }
            else
            {
                BackendReturnObject insertResult = null;
                Backend.GameData.Insert(TableName, BuildParam(character), response => insertResult = response);
                yield return new WaitUntil(() => insertResult != null);

                if (!insertResult.IsSuccess())
                {
                    Debug.LogError($"[CharacterRepository] UserData 생성 실패: {insertResult.GetMessage()}");
                    yield return RestoreAccountAfterCharacterFailure("초기 플레이 정보를 저장하지 못했습니다.", completed);
                    yield break;
                }

                character.rowInDate = insertResult.GetInDate();
            }

            selectedCharacter = character;
            revision = 0;
            savedRevision = 0;
            saveInProgress = false;
            lastSelectedId = character.id;
            completed(true, null, character);
        }

        static IEnumerator RestoreAccountAfterCharacterFailure(string dataError, Action<bool, string, CharacterData> completed)
        {
            bool restored = false;
            string restoreError = null;
            yield return ReturnToAccount((success, error) =>
            {
                restored = success;
                restoreError = error;
            });
            completed(false, restored ? dataError : restoreError, null);
        }

        public static void MarkDirty()
        {
            if (selectedCharacter != null) revision++;
        }

        public static IEnumerator SaveSelected(Action<bool, string> completed)
        {
            while (saveInProgress) yield return null;

            if (selectedCharacter == null)
            {
                completed(false, "저장할 캐릭터가 없습니다.");
                yield break;
            }

            if (revision == savedRevision)
            {
                completed(true, null);
                yield break;
            }

            saveInProgress = true;
            int savingRevision = revision;
            BackendReturnObject result = null;
            Param param = BuildParam(selectedCharacter);

            if (string.IsNullOrEmpty(selectedCharacter.rowInDate))
                Backend.GameData.Insert(TableName, param, response => result = response);
            else
                Backend.GameData.UpdateV2(TableName, selectedCharacter.rowInDate, Backend.UserInDate, param, response => result = response);

            yield return new WaitUntil(() => result != null);
            saveInProgress = false;

            if (!result.IsSuccess())
            {
                Debug.LogError($"[CharacterRepository] UserData 저장 실패: {result.GetMessage()}");
                completed(false, "플레이 정보 저장에 실패했습니다.");
                yield break;
            }

            if (string.IsNullOrEmpty(selectedCharacter.rowInDate))
                selectedCharacter.rowInDate = result.GetInDate();
            savedRevision = savingRevision;
            completed(true, null);
        }

        public static IEnumerator ReturnToAccount(Action<bool, string> completed)
        {
            BackendReturnObject logoutResult = null;
            Backend.BMember.Logout(response => logoutResult = response);
            yield return new WaitUntil(() => logoutResult != null);

            if (!logoutResult.IsSuccess())
                Debug.LogWarning($"[CharacterRepository] 캐릭터 로그아웃 실패, 계정 재로그인 시도: {logoutResult.GetMessage()}");

            bool authenticated = false;
            string authenticationError = null;
            yield return Authenticate(false, (success, error) =>
            {
                authenticated = success;
                authenticationError = error;
            });

            if (authenticated)
            {
                selectedCharacter = null;
                revision = 0;
                savedRevision = 0;
                saveInProgress = false;
            }

            completed(authenticated, authenticationError);
        }

        static Param BuildParam(CharacterData character)
        {
            var param = new Param();
            param.Add("portraitIndex", character.portraitIndex);
            param.Add("stage", character.stage);
            param.Add("killsInStage", character.killsInStage);
            param.Add("gold", character.gold);
            param.Add("attackLevel", character.attackLevel);
            param.Add("healthLevel", character.healthLevel);
            param.Add("defenseLevel", character.defenseLevel);
            param.Add("critLevel", character.critLevel);
            param.Add("goldGainLevel", character.goldGainLevel);
            return param;
        }

        static void ApplyRow(CharacterData character, JsonData row)
        {
            if (row == null || !row.IsObject) return;

            character.rowInDate = ReadString(row, "inDate", character.rowInDate);
            character.portraitIndex = Math.Max(0, ReadInt(row, "portraitIndex", character.portraitIndex));
            character.stage = Math.Max(1, ReadInt(row, "stage", character.stage));
            character.killsInStage = Mathf.Clamp(ReadInt(row, "killsInStage", character.killsInStage), 0, CharacterData.KillsPerStage - 1);
            character.gold = Math.Max(0L, ReadLong(row, "gold", character.gold));
            character.attackLevel = Math.Max(1, ReadInt(row, "attackLevel", character.attackLevel));
            character.healthLevel = Math.Max(1, ReadInt(row, "healthLevel", character.healthLevel));
            character.defenseLevel = Math.Max(1, ReadInt(row, "defenseLevel", character.defenseLevel));
            character.critLevel = Math.Max(1, ReadInt(row, "critLevel", character.critLevel));
            character.goldGainLevel = Math.Max(1, ReadInt(row, "goldGainLevel", character.goldGainLevel));
        }

        static string ReadString(JsonData row, string key, string fallback)
        {
            return row != null && row.IsObject && row.ContainsKey(key) && row[key] != null ? row[key].ToString() : fallback;
        }

        static int ReadInt(JsonData row, string key, int fallback)
        {
            return int.TryParse(ReadString(row, key, null), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        }

        static long ReadLong(JsonData row, string key, long fallback)
        {
            return long.TryParse(ReadString(row, key, null), NumberStyles.Integer, CultureInfo.InvariantCulture, out long value) ? value : fallback;
        }
    }
}
