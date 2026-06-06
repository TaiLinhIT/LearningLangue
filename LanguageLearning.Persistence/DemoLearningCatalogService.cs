using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;

namespace LanguageLearning.Persistence;

public class DemoLearningCatalogService : ILearningCatalogService
{
    private readonly IReadOnlyList<LanguageOption> _languages =
    [
        new("en", "Tieng Anh", "English", "US", "Giao tiep quoc te, hoc thuat va cong viec.", 12840, "De bat dau"),
        new("ja", "Tieng Nhat", "日本語", "JP", "Tu kana, kaiwa den JLPT N5-N2.", 6420, "Can kien tri"),
        new("ko", "Tieng Han", "한국어", "KR", "Hangul, hoi thoai, van hoa va TOPIK.", 5180, "Vua phai"),
        new("zh", "Tieng Trung", "中文", "CN", "Pinyin, chu Han, HSK va giao tiep.", 7340, "Vua phai"),
        new("fr", "Tieng Phap", "Francais", "FR", "Phat am, ngu phap va dam thoai hang ngay.", 2860, "Lang man")
    ];

    private readonly IReadOnlyList<Lesson> _lessons =
    [
        new(1, "Chao hoi va gioi thieu ban than", "en", "A1", "Giao tiep", "Noi", 15, false,
            "Hoc cach chao hoi, noi ten, nghe cau hoi co ban va tra loi lich su.",
            ["Hello, my name is Linh.", "Nice to meet you.", "Where are you from?"],
            [
                new("Cau nao dung de tu gioi thieu ten?", ["I am coffee.", "My name is An.", "She from Japan."], 1, "My name is... la mau cau gioi thieu ten tu nhien."),
                new("Nice to meet you nghia la gi?", ["Rat vui duoc gap ban", "Tam biet", "Toi dang hoc"], 0, "Day la cau dap lich su khi gap lan dau.")
            ]),
        new(2, "Tu vung cong viec hang ngay", "en", "A2", "Cong viec", "Tu vung", 18, false,
            "Mo rong tu vung ve lich hop, email, deadline va trao doi trong van phong.",
            ["I have a meeting at nine.", "Please send the report.", "The deadline is Friday."],
            [
                new("Deadline thuong noi ve dieu gi?", ["Han chot", "Ngay nghi", "Phong hop"], 0, "Deadline la han can hoan thanh cong viec."),
                new("Report co nghia la gi?", ["Bao cao", "Hoa don", "May tinh"], 0, "Report la bao cao.")
            ]),
        new(3, "Ngu phap thi hien tai hoan thanh", "en", "B1", "Ngu phap", "Ngu phap", 22, true,
            "Nam cau truc have/has + V3 de noi ve trai nghiem va ket qua hien tai.",
            ["I have visited Seoul.", "She has finished the lesson.", "Have you tried sushi?"],
            [
                new("Chon cau dung.", ["She have finished.", "She has finished.", "She has finish."], 1, "Chu ngu she di voi has va dong tu phan tu qua khu."),
                new("Have you ever...? dung de hoi ve", ["Thoi tiet", "Trai nghiem", "So huu"], 1, "Ever thuong dung khi hoi trai nghiem trong doi.")
            ]),
        new(4, "Hiragana co ban", "ja", "A1", "Bang chu cai", "Doc", 20, false,
            "Lam quen nguyen am va cach doc cac am tiet dau tien trong tieng Nhat.",
            ["あ = a", "い = i", "う = u"],
            [
                new("あ doc la gi?", ["a", "i", "u"], 0, "あ la nguyen am a."),
                new("い doc la gi?", ["e", "i", "o"], 1, "い la nguyen am i.")
            ]),
        new(5, "Hangul va cau chao", "ko", "A1", "Giao tiep", "Phat am", 17, false,
            "Tap doc Hangul va noi cac cau chao thuong dung trong tieng Han.",
            ["안녕하세요", "감사합니다", "또 만나요"],
            [
                new("안녕하세요 dung khi nao?", ["Chao hoi", "Xin loi", "Dat mon"], 0, "Day la cau chao pho bien va lich su."),
                new("감사합니다 nghia la gi?", ["Cam on", "Tam biet", "Rat dat"], 0, "Day la cach noi cam on lich su.")
            ]),
        new(6, "Pinyin va thanh dieu", "zh", "A1", "Phat am", "Nghe", 19, false,
            "Nhan dien bốn thanh dieu co ban va luyen nghe am tiet ngan.",
            ["ma1", "ma2", "ma3", "ma4"],
            [
                new("Pinyin dung de lam gi?", ["Ghi am doc", "Tinh diem", "Dich tien"], 0, "Pinyin giup nguoi hoc doc am tieng Trung."),
                new("Tieng Trung pho thong co may thanh dieu chinh?", ["2", "4", "7"], 1, "Co bon thanh dieu chinh.")
            ])
    ];

    private readonly IReadOnlyList<VocabularyItem> _vocabulary =
    [
        new("journey", "hanh trinh", "/ˈdʒɜːrni/", "Learning a language is a long journey.", "en", "Dong luc", true),
        new("confident", "tu tin", "/ˈkɑːnfɪdənt/", "She feels confident speaking English.", "en", "Cam xuc", false),
        new("meeting", "cuoc hop", "/ˈmiːtɪŋ/", "The meeting starts at 9 AM.", "en", "Cong viec", true),
        new("ありがとう", "cam on", "arigatou", "ありがとう、またね。", "ja", "Giao tiep", false),
        new("학교", "truong hoc", "hak-gyo", "저는 학교에 가요.", "ko", "Hoc tap", false),
        new("朋友", "ban be", "pengyou", "他是我的朋友。", "zh", "Quan he", false),
        new("bonjour", "xin chao", "/bɔ̃ʒuʁ/", "Bonjour, comment ca va?", "fr", "Giao tiep", false)
    ];

    private readonly IReadOnlyList<PlacementQuestion> _placementQuestions =
    [
        new("Chon cau dung: She ___ a book every night.", ["read", "reads", "reading", "to read"], 1, "Ngu phap"),
        new("Tu nao gan nghia voi quickly?", ["slowly", "fast", "late", "quiet"], 1, "Tu vung"),
        new("Chon cau hoi tu nhien de hoi gia tien.", ["How much is it?", "How many it is?", "What price this?", "Where cost?"], 0, "Noi"),
        new("I have lived here ___ 2022.", ["for", "since", "at", "on"], 1, "Ngu phap"),
        new("Cau nao phu hop trong email cong viec?", ["Yo!", "Dear Ms. Lan,", "Hey boss!!!", "What up?"], 1, "Viet")
    ];

    public IReadOnlyList<LanguageOption> GetLanguages() => _languages;

    public IReadOnlyList<Lesson> GetLessons(string? languageCode = null) =>
        string.IsNullOrWhiteSpace(languageCode)
            ? _lessons
            : _lessons.Where(lesson => lesson.LanguageCode == languageCode).ToList();

    public Lesson? GetLesson(int id) => _lessons.FirstOrDefault(lesson => lesson.Id == id);

    public IReadOnlyList<VocabularyItem> GetVocabulary(string? query = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _vocabulary;
        }

        return _vocabulary
            .Where(item => item.Term.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Meaning.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Topic.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<PlacementQuestion> GetPlacementQuestions() => _placementQuestions;

    public LearningProgress GetProgress() =>
        new("A2", 18, 42, 86, 12, 247,
        [
            new("Tu vung", "Aa", 72, "On tap bang flashcard va cau vi du."),
            new("Ngu phap", "()", 58, "Mau cau, thi, cau truc ung dung."),
            new("Nghe", ">>", 46, "Audio ngan, chep chinh ta va dien tu."),
            new("Noi", "Mic", 39, "Ghi am va so sanh muc do tu tin."),
            new("Doc", "Doc", 64, "Doc hieu theo cap do CEFR."),
            new("Viet", "But", 31, "Viet cau, doan ngan va nhan feedback.")
        ]);

    public IReadOnlyList<PricingPlan> GetPricingPlans() =>
    [
        new("Free", "0d", "Hoc thu mien phi va theo doi tien do co ban.",
            ["3 bai hoc moi tuan", "Flashcard co ban", "Quiz cuoi bai"], false),
        new("Plus", "149.000d/thang", "Mo khoa tat ca bai hoc va luyen nghe noi nang cao.",
            ["Tat ca khoa hoc", "Audio/video khong gioi han", "Bai test chuong", "Lo trinh ca nhan"], true),
        new("Team", "Lien he", "Danh cho lop hoc, trung tam hoac doanh nghiep.",
            ["Quan ly nhom", "Bao cao hoc tap", "Tai khoan giao vien", "Ho tro noi dung rieng"], false)
    ];

    public IReadOnlyList<AdminMetric> GetAdminMetrics() =>
    [
        new("Nguoi dung", "18.240", "+12% thang nay"),
        new("Khoa hoc", "36", "+4 khoa moi"),
        new("Bai hoc", "428", "+31 bai moi"),
        new("Doanh thu", "268tr", "+18% thang nay")
    ];
}
