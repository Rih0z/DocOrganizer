using System;
using System.Linq;
using DocOrganizer.Core.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace DocOrganizer.Tests.TestHelpers
{
    /// <summary>
    /// PdfDocument用のFluentAssertions拡張
    /// </summary>
    public static class PdfAssertions
    {
        /// <summary>
        /// PdfDocumentのアサーションヘルパー
        /// </summary>
        public static PdfDocumentAssertions Should(this PdfDocument document)
        {
            return new PdfDocumentAssertions(document);
        }
    }

    /// <summary>
    /// PdfDocumentのカスタムアサーション
    /// </summary>
    public class PdfDocumentAssertions : ReferenceTypeAssertions<PdfDocument, PdfDocumentAssertions>
    {
        public PdfDocumentAssertions(PdfDocument document)
            : base(document)
        {
        }

        protected override string Identifier => "PdfDocument";

        /// <summary>
        /// ページ数が指定値であることを検証
        /// </summary>
        public AndConstraint<PdfDocumentAssertions> HavePageCount(int expected, string because = "")
        {
            Execute.Assertion
                .BecauseOf(because)
                .Given(() => Subject.Pages)
                .ForCondition(pages => pages.Count == expected)
                .FailWith("Expected {context:PdfDocument} to have {0} page(s){reason}, but found {1}.",
                    expected, Subject.Pages.Count);

            return new AndConstraint<PdfDocumentAssertions>(this);
        }

        /// <summary>
        /// 指定ページの回転角度が期待値であることを検証
        /// </summary>
        public AndConstraint<PdfDocumentAssertions> HavePageRotation(int pageIndex, int expectedRotation, string because = "")
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(pageIndex >= 0 && pageIndex < Subject.Pages.Count)
                .FailWith("Expected page index {0} to be within range{reason}, but there are only {1} pages.",
                    pageIndex, Subject.Pages.Count);

            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(Subject.Pages[pageIndex].Rotation == expectedRotation)
                .FailWith("Expected page {0} to have rotation {1}{reason}, but found {2}.",
                    pageIndex, expectedRotation, Subject.Pages[pageIndex].Rotation);

            return new AndConstraint<PdfDocumentAssertions>(this);
        }

        /// <summary>
        /// 全ページがA4サイズであることを検証（10pxの誤差許容）
        /// </summary>
        public AndConstraint<PdfDocumentAssertions> HaveA4Pages(string because = "")
        {
            const float A4Width = 595;
            const float A4Height = 842;
            const float Tolerance = 10;

            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(Subject.Pages.All(p =>
                    Math.Abs(p.Width - A4Width) < Tolerance &&
                    Math.Abs(p.Height - A4Height) < Tolerance))
                .FailWith("Expected all pages to be A4 size (595x842 ±10px){reason}, but some pages have different sizes.");

            return new AndConstraint<PdfDocumentAssertions>(this);
        }

        /// <summary>
        /// 全ページの幅・高さが正の値であることを検証
        /// </summary>
        public AndConstraint<PdfDocumentAssertions> HaveValidPageDimensions(string because = "")
        {
            Execute.Assertion
                .BecauseOf(because)
                .ForCondition(Subject.Pages.All(p => p.Width > 0 && p.Height > 0))
                .FailWith("Expected all pages to have positive width and height{reason}, but some pages have invalid dimensions.");

            return new AndConstraint<PdfDocumentAssertions>(this);
        }
    }
}
