import React, { Component } from "react";
import { connect } from "react-redux";
import { Link } from "react-router-dom";
import { bindActionCreators } from "redux";
import { sleepAsync } from "../common/functions";
import { actionCreators } from "../store/WikiWalks";
import Head from "./Helmet";

class Category extends Component {
    sectionStyle = {
        display: "block",
        borderTop: "1px solid #dcdcdc",
        paddingTop: 12,
        marginTop: 12,
    };

    constructor(props) {
        super(props);

        const originalCategory = this.props.match.params.category.split("#")[0];
        const encodedCategory = originalCategory.split(",").join("%2C");
        if (originalCategory !== encodedCategory) {
            //If the comma was not encoded, use encoded URL to prevent the duplication of the pages
            window.location.href = `/category/${encodedCategory}`;
        }

        if (window.location.pathname.split("#")[0].includes("%27")) {
            //基本的にはエンコードされたURLを正とするが、react-routerの仕様上、
            //「%27」のみは「'」を正とする。
            window.location.href = window.location.pathname
                .split("%27")
                .join("'");
        }

        const category = decodeURIComponent(
            originalCategory.split("_").join(" ")
        );

        this.state = {
            pages: [],
            category,
        };
    }

    componentDidMount() {
        const getData = async () => {
            const url = `api/WikiWalks/getWordsForCategoryWithoutSnippet?category=${encodeURIComponent(
                this.state.category
            )}&top=100`;
            const response = await fetch(url);
            const { pages, hasMore } = await response.json();
            this.setState({ pages });

            if (hasMore) {
                await sleepAsync(1000);

                const urlAll = `api/WikiWalks/getWordsForCategoryWithoutSnippet?category=${encodeURIComponent(
                    this.state.category
                )}`;
                const responseAll = await fetch(urlAll);
                const { pages } = await responseAll.json();
                this.setState({ pages });
            }
        };
        getData();
    }

    render() {
        const { pages, category } = this.state;
        const strCat = category ? category.split('"').join("") : "...";
        const description = `This is a list of the keywords about ${strCat}. Please check the words below to learn about ${strCat}!`;
        const lineChangeDesc = (
            <div>
                {"This is a list of the keywords about "}
                <span style={{ fontWeight: "bold" }}>{strCat}</span>
                {"."}
                <br />
                {"Please check the words below to learn about "}
                <span style={{ fontWeight: "bold" }}>{strCat}</span>
                {"!"}
            </div>
        );
        return (
            <div>
                <Head
                    title={category}
                    desc={description}
                    noindex={!(category || "").toLowerCase().includes("japan")}
                />
                <div
                    className="breadcrumbs"
                    itemScope
                    itemType="https://schema.org/BreadcrumbList"
                    style={{ textAlign: "left" }}
                >
                    <span
                        itemProp="itemListElement"
                        itemScope
                        itemType="http://schema.org/ListItem"
                    >
                        <Link
                            to="/"
                            itemProp="item"
                            style={{ marginRight: "5px", marginLeft: "5px" }}
                        >
                            <span itemProp="name">{"Home"}</span>
                        </Link>
                        <meta itemProp="position" content="1" />
                    </span>
                    {" > "}
                    <span
                        itemProp="itemListElement"
                        itemScope
                        itemType="http://schema.org/ListItem"
                    >
                        <span
                            itemProp="name"
                            style={{ marginRight: "5px", marginLeft: "5px" }}
                        >
                            {category}
                        </span>
                        <meta itemProp="position" content="2" />
                    </span>
                </div>
                <section style={this.sectionStyle}>
                    <h1>{category}</h1>
                    <br />
                    {lineChangeDesc}
                    <br />
                    <section style={this.sectionStyle}>
                        <h2>{`Keywords about ${category}`}</h2>
                        <table className="table table-striped">
                            <thead>
                                <tr>
                                    <th style={{ verticalAlign: "middle" }}>
                                        Keywords
                                    </th>
                                    <th>
                                        <span
                                            style={{
                                                display: "inline-block",
                                                minWidth: 70,
                                            }}
                                        >
                                            Found Articles
                                        </span>
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                {pages.length > 0 ? (
                                    pages
                                        .sort(
                                            (p1, p2) =>
                                                p2.referenceCount -
                                                p1.referenceCount
                                        )
                                        .filter(page => page.referenceCount > 4)
                                        .map(page => (
                                            <tr key={page.wordId}>
                                                <td>
                                                    <Link
                                                        to={
                                                            "/word/" +
                                                            page.wordId
                                                        }
                                                    >
                                                        {page.word}
                                                    </Link>
                                                </td>
                                                <td>
                                                    {page.referenceCount} pages
                                                </td>
                                            </tr>
                                        ))
                                ) : (
                                    <tr>
                                        <td>Loading...</td>
                                        <td></td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </section>
                </section>
            </div>
        );
    }
}

export default connect(
    state => state.wikiWalks,
    dispatch => bindActionCreators(actionCreators, dispatch)
)(Category);
