import Foundation

struct CaseGroup: Identifiable, Hashable {
    var usdzModelNames: [String]
    var name: String
    var description: String
    var id: String { primaryModel } // ID used for List selection binding
    var primaryModel: String { usdzModelNames.first ?? "" }
}

struct DummyFragmentData {
    static let caseGroups: [CaseGroup] = [
        CaseGroup(
            usdzModelNames: ["CATALUNYA-mandibula.usdz"],
            name: "Case 1 Tabrakan",
            description: "Kasus Tabrakan"
        ),
        CaseGroup(
            usdzModelNames: ["CHARIS-mandibula.usdz"],
            name: "Case 2 Tumor",
            description: "Kasus Tumor"
        ),
    ]
}
