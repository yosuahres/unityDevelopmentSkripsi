//casegroup.swift
import Foundation
import RealityKit

struct CaseGroup: Identifiable, Hashable {
    var usdzModelNames: [String]
    var name: String
    var description: String
    var id: String { primaryModel } 
    var primaryModel: String { usdzModelNames.first ?? "" }
}
