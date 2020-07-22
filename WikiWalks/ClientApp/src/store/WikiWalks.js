const initializeType = 'INITIALIZE';
const receiveWordType = 'RECEIVE_WORD';
const receivePagesType = 'RECEIVE_PAGES';
const initialState = { pages: {}, word: "Loading..." };

export const actionCreators = {
    requestWord: wordId => async dispatch => {
        try {
            const url = `api/WikiWalks/getWord?wordId=${wordId}`;
            const response = await fetch(url);
            const { word } = await response.json();

            if (!word) window.location.href = `/not-found?p=${window.location.pathname}`;

            dispatch({ type: receiveWordType, word });
        } catch (e) {
            window.location.href = `/not-found?p=${window.location.pathname}`;
        }
    },
    requestPagesForTheTitle: wordId => async dispatch => {
        try {
            const url = `api/WikiWalks/getRelatedArticles?wordId=${wordId}`;
            const response = await fetch(url);
            const pages = await response.json();

            if (!pages || !pages.pages || pages.pages.length < 5) window.location.href = `/not-found?p=${window.location.pathname}`;

            dispatch({ type: receivePagesType, pages });
        } catch (e) {
            window.location.href = `/not-found?p=${window.location.pathname}`;
        }
    },
    initialize: () => dispatch => {
        dispatch({ type: initializeType });
    }
};

export const reducer = (state, action) => {
    state = state || initialState;

    if (action.type === initializeType) {
        return initialState;
    }

    if (action.type === receiveWordType) {
        return {
            ...state,
            word: action.word,
        };
    }

    if (action.type === receivePagesType) {
        return {
            ...state,
            pages: action.pages,
        };
    }

    return state;
};
